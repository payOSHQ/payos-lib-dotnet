using System.Collections.Generic;
using System.Threading.Tasks;

using PayOS.Models;
using PayOS.Models.V1.Payouts;
using PayOS.Models.V1.Payouts.Batch;

namespace PayOS.Resources.V1.Payouts.Batch;

/// <summary>
/// Batch payout resource for managing batch payout operations
/// </summary>
public class Batch(PayOSClient client) : ApiResource(client)
{
    /// <summary>
    /// Create a batch payout
    /// </summary>
    /// <param name="payoutData">The details of batch payout to be created</param>
    /// <param name="idempotencyKey">A unique key for ensuring idempotency. Use a UUID or other high-entropy string so that retries of the same request are recognized and duplicated payouts are prevented</param>
    /// <param name="requestOptions">Request options</param>
    /// <returns>A promise that resolves to the newly created Payout object</returns>
    public async Task<Payout> CreateAsync(
        PayoutBatchRequest payoutData,
        string? idempotencyKey = null,
        RequestOptions<PayoutBatchRequest>? requestOptions = null)
    {
        idempotencyKey ??= _client.Crypto.CreateUuidv4();
        var signatureOptions = new SignatureOptions
        {
            Request = RequestSignatureTypes.Header,
            Response = ResponseSignatureTypes.Header
        };
        var headers = new Dictionary<string, string>
        {
            { "x-idempotency-key", idempotencyKey }
        };
        if (requestOptions?.Headers != null)
        {
            foreach (var header in requestOptions.Headers)
            {
                headers[header.Key] = header.Value;
            }
        }
        var finalRequestOptions = new RequestOptions<PayoutBatchRequest>
        {
            Signature = requestOptions?.Signature ?? signatureOptions,
            Headers = headers,
            Body = payoutData,
            Query = requestOptions?.Query,
            MaxRetries = requestOptions?.MaxRetries,
            TimeoutMs = requestOptions?.TimeoutMs,
            CancellationToken = requestOptions?.CancellationToken ?? default
        };
        return await _client.PostAsync<Payout, PayoutBatchRequest>(
            "/v1/payouts/batch",
            finalRequestOptions
        );
    }
}