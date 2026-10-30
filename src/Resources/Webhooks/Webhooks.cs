using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using PayOS.Exceptions;
using PayOS.Internal;
using PayOS.Models;
using PayOS.Models.Webhooks;

namespace PayOS.Resources.Webhooks;


/// <summary>
/// Webhooks resource for managing webhook operations
/// </summary>
public class Webhooks(PayOSClient client) : ApiResource(client)
{

    /// <summary>
    /// Validate and register a webhook URL with payOS.
    /// payOS will test the webhook endpoint by sending a validation request to it.
    /// If the webhook responds correctly, it will be registered for payment notifications.
    /// </summary>
    /// <param name="webhookUrl">Your webhook endpoint URL that will receive payment notifications</param>
    /// <param name="requestOptions">Request options</param>
    /// <returns>The confirmed webhook URL result</returns>
    /// <exception cref="WebhookException">When webhook URL is invalid or validation fails</exception>
    public async Task<ConfirmWebhookResponse> ConfirmAsync(
        string webhookUrl,
        RequestOptions<ConfirmWebhookRequest>? requestOptions = null)
    {
        if (string.IsNullOrEmpty(webhookUrl))
        {
            throw new WebhookException("Webhook URL invalid.");
        }

        try
        {
            var body = new ConfirmWebhookRequest
            {
                WebhookUrl = webhookUrl
            };
            var finalRequestOptions = new RequestOptions<ConfirmWebhookRequest>
            {
                Signature = requestOptions?.Signature,
                Headers = requestOptions?.Headers,
                Body = body,
                Query = requestOptions?.Query,
                MaxRetries = requestOptions?.MaxRetries,
                TimeoutMs = requestOptions?.TimeoutMs,
                CancellationToken = requestOptions?.CancellationToken ?? default
            };
            return await _client.PostAsync<ConfirmWebhookResponse, ConfirmWebhookRequest>(
                "/confirm-webhook",
                finalRequestOptions
            );
        }
        catch (PayOSException ex)
        {
            throw new WebhookException($"Webhook validation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Verify the webhook data sent from payOS
    /// </summary>
    /// <param name="webhook">The request body received from payOS</param>
    /// <returns>The verified webhook data</returns>
    /// <exception cref="WebhookException">When webhook verification fails</exception>
    public async Task<WebhookData> VerifyAsync(Webhook webhook)
    {
        return await Task.Run(() =>
        {

            if (webhook?.Data == null)
            {
                throw new WebhookException("Invalid webhook data");
            }

            if (string.IsNullOrEmpty(webhook.Signature))
            {
                throw new WebhookException("Invalid signature");
            }

            WebhookValidation.ValidateTyped(webhook.Data);

            var signedSignature = _client.Crypto.CreateSignatureFromObject(webhook.Data, _client.ChecksumKey);

            return string.IsNullOrEmpty(signedSignature) || !WebhookValidation.ConstantTimeEquals(signedSignature, webhook.Signature)
                ? throw new WebhookException("Data not integrity")
                : webhook.Data;
        });
    }

    /// <summary>
    /// Verify a webhook from its raw JSON request body. Recommended over the typed overload:
    /// it validates the data exactly as sent before hashing it.
    /// </summary>
    /// <param name="webhookJson">The raw request body received from payOS</param>
    /// <returns>The verified webhook data</returns>
    /// <exception cref="WebhookException">When webhook verification fails</exception>
    public async Task<WebhookData> VerifyAsync(string webhookJson)
    {
        return await Task.Run(() =>
        {
            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(webhookJson);
            }
            catch (Exception ex) when (ex is JsonException || ex is ArgumentException)
            {
                throw new WebhookException("Invalid webhook data: malformed JSON");
            }

            using (document)
            {
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                {
                    throw new WebhookException("Invalid webhook data");
                }

                // TryGetProperty silently picks one of several duplicates, so reject them first.
                // Names and strings are decoded lazily: an unpaired surrogate only fails here.
                var seen = new HashSet<string>(StringComparer.Ordinal);
                try
                {
                    foreach (var property in root.EnumerateObject())
                    {
                        if (!seen.Add(property.Name))
                        {
                            throw new WebhookException($"Invalid webhook data: duplicate key {property.Name}");
                        }
                    }
                }
                catch (Exception ex) when (ex is InvalidOperationException || ex is JsonException)
                {
                    throw new WebhookException("Invalid webhook data: malformed JSON");
                }

                if (!root.TryGetProperty("data", out var data) || data.ValueKind == JsonValueKind.Null)
                {
                    throw new WebhookException("Invalid webhook data");
                }

                try
                {
                    WebhookValidation.ValidateWireData(data);
                }
                catch (Exception ex) when (ex is InvalidOperationException || ex is JsonException)
                {
                    throw new WebhookException("Invalid webhook data: malformed JSON");
                }

                if (!root.TryGetProperty("signature", out var sig) || sig.ValueKind != JsonValueKind.String)
                {
                    throw new WebhookException("Invalid signature");
                }

                string? signature;
                try
                {
                    signature = sig.GetString();
                }
                catch (InvalidOperationException)
                {
                    throw new WebhookException("Invalid signature");
                }

                if (string.IsNullOrEmpty(signature))
                {
                    throw new WebhookException("Invalid signature");
                }

                var signed = _client.Crypto.CreateSignatureFromObject(data, _client.ChecksumKey);
                if (string.IsNullOrEmpty(signed) || !WebhookValidation.ConstantTimeEquals(signed, signature))
                {
                    throw new WebhookException("Data not integrity");
                }

                try
                {
                    return JsonSerializer.Deserialize<WebhookData>(data.GetRawText())!;
                }
                catch (JsonException)
                {
                    throw new WebhookException("Invalid webhook data");
                }
            }
        });
    }
}