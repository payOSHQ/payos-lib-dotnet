using System.Threading;
using System.Threading.Tasks;

using PayOS.Exceptions;
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

            var signedSignature = _client.Crypto.CreateSignatureFromObject(webhook.Data, _client.ChecksumKey);

            return string.IsNullOrEmpty(signedSignature) || signedSignature != webhook.Signature
                ? throw new WebhookException("Data not integrity")
                : webhook.Data;
        });
    }
}