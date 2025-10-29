using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using PayOS.Internal;
using PayOS.Models;
using PayOS.Models.V1.Payouts;
using PayOS.Models.V1.Payouts.Batch;

namespace PayOS.Resources.V1.Payouts;


/// <summary>
/// Payouts resource for managing payout operations
/// </summary>
public class Payouts(PayOSClient client) : ApiResource(client)
{
    /// <summary>
    /// Batch payout operations
    /// </summary>
    public Batch.Batch Batch { get; } = new(client);

    /// <summary>
    /// Create a new payout
    /// </summary>
    /// <param name="payoutData">The details of the payout to be created</param>
    /// <param name="idempotencyKey">A unique key for ensuring idempotency. Use a UUID or other high-entropy string so that retries of the same request are recognized and duplicated payouts are prevented</param>
    /// <param name="requestOptions">Request options</param>
    /// <returns>A promise that resolves to the newly created Payout object</returns>
    public async Task<Payout> CreateAsync(
        PayoutRequest payoutData,
        string? idempotencyKey = null,
        RequestOptions<Payout>? requestOptions = null
        )
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
        var finalRequestOptions = new RequestOptions<PayoutRequest>
        {
            Signature = requestOptions?.Signature ?? signatureOptions,
            Headers = headers,
            Body = payoutData,
            Query = requestOptions?.Query,
            MaxRetries = requestOptions?.MaxRetries,
            TimeoutMs = requestOptions?.TimeoutMs,
            CancellationToken = requestOptions?.CancellationToken ?? default
        };
        return await _client.PostAsync<Payout, PayoutRequest>(
            "/v1/payouts/",
            finalRequestOptions
        );
    }

    /// <summary>
    /// Retrieves detailed information about a specific payout
    /// </summary>
    /// <param name="payoutId">The unique identifier of the payout to retrieve</param>
    /// <param name="requestOptions">Request options</param>
    /// <returns>A promise that resolves to the Payout object containing the payout details</returns>
    public async Task<Payout> GetAsync(
        string payoutId,
        RequestOptions? requestOptions = null)
    {
        var signatureOptions = new SignatureOptions
        {
            Response = ResponseSignatureTypes.Header
        };
        var finalRequestOptions = new RequestOptions<object>
        {
            Signature = requestOptions?.Signature ?? signatureOptions,
            Headers = requestOptions?.Headers,
            Query = requestOptions?.Query,
            Body = null,
            MaxRetries = requestOptions?.MaxRetries,
            TimeoutMs = requestOptions?.TimeoutMs,
            CancellationToken = requestOptions?.CancellationToken ?? default
        };
        return await _client.GetAsync<Payout>(
            $"/v1/payouts/{payoutId}",
            finalRequestOptions
        );
    }

    /// <summary>
    /// Retrieves a paginated list of payouts with optional filtering
    /// </summary>
    /// <param name="param">Filter parameters with optional limit and starting offset</param>
    /// <param name="requestOptions">Request options</param>
    /// <returns>A Page object that supports both iteration and manual page navigation</returns>
    public async Task<Page<Payout, PayoutListResponse>> ListAsync(
        GetPayoutListParam? param = null,
        RequestOptions? requestOptions = null)
    {
        var pageSize = param?.Limit ?? 50;
        var startingOffset = param?.Offset ?? 0;

        async Task<PayoutListResponse> FetchPage(int offset, int limit, CancellationToken cancellationToken)
        {
            var queryParams = new Dictionary<string, object>();
            var signatureOptions = new SignatureOptions
            {
                Response = ResponseSignatureTypes.Header
            };

            if (param != null)
            {
                if (!string.IsNullOrEmpty(param.ReferenceId))
                    queryParams["referenceId"] = param.ReferenceId!;
                if (param.ApprovalState.HasValue)
                    queryParams["approvalState"] = param.ApprovalState;
                if (param.Category != null && param.Category.Count != 0)
                    queryParams["category"] = string.Join(",", param.Category);
                if (!string.IsNullOrEmpty(param.FromDate))
                    queryParams["fromDate"] = param.FromDate!;
                if (!string.IsNullOrEmpty(param.ToDate))
                    queryParams["toDate"] = param.ToDate!;
            }

            queryParams["limit"] = limit;
            queryParams["offset"] = offset;

            var modifiedRequestOptions = new RequestOptions<object>
            {
                Signature = requestOptions?.Signature ?? signatureOptions,
                Headers = requestOptions?.Headers,
                Query = queryParams,
                Body = null,
                MaxRetries = requestOptions?.MaxRetries,
                TimeoutMs = requestOptions?.TimeoutMs,
                CancellationToken = cancellationToken
            };

            return await _client.GetAsync<PayoutListResponse>(
                "/v1/payouts",
                modifiedRequestOptions
            );
        }

        var initialResponse = await FetchPage(startingOffset, pageSize, requestOptions?.CancellationToken ?? default);

        return PaginationExtensions.CreatePage(
            initialResponse,
            FetchPage,
            response => response.Pagination,
            response => response.Payouts
        );
    }

    /// <summary>
    /// Estimate credit of payouts
    /// </summary>
    /// <param name="payoutData">The details of the payout to be estimated</param>
    /// <param name="requestOptions">Request options</param>
    /// <returns>Estimate credit result</returns>
    public async Task<EstimateCredit> EstimateCredit(PayoutRequest payoutData, RequestOptions? requestOptions = null)
    {
        var signatureOptions = new SignatureOptions
        {
            Request = RequestSignatureTypes.Header
        };
        var finalRequestOptions = new RequestOptions<PayoutRequest>
        {
            Signature = requestOptions?.Signature ?? signatureOptions,
            Headers = requestOptions?.Headers,
            Query = requestOptions?.Query,
            Body = payoutData,
            MaxRetries = requestOptions?.MaxRetries,
            TimeoutMs = requestOptions?.TimeoutMs,
            CancellationToken = requestOptions?.CancellationToken ?? default
        };
        return await _client.PostAsync<EstimateCredit, PayoutRequest>(
            $"/v1/payouts/estimate-credit",
            finalRequestOptions
        );
    }

    /// <summary>
    /// Estimate credit of payouts
    /// </summary>
    /// <param name="payoutData">The details of the payout to be estimated</param>
    /// <param name="requestOptions">Request options</param>
    /// <returns>Estimate credit result</returns>
    public async Task<EstimateCredit> EstimateCredit(PayoutBatchRequest payoutData, RequestOptions? requestOptions = null)
    {
        var signatureOptions = new SignatureOptions
        {
            Request = RequestSignatureTypes.Header
        };
        var finalRequestOptions = new RequestOptions<PayoutBatchRequest>
        {
            Signature = requestOptions?.Signature ?? signatureOptions,
            Headers = requestOptions?.Headers,
            Query = requestOptions?.Query,
            Body = payoutData,
            MaxRetries = requestOptions?.MaxRetries,
            TimeoutMs = requestOptions?.TimeoutMs,
            CancellationToken = requestOptions?.CancellationToken ?? default
        };
        return await _client.PostAsync<EstimateCredit, PayoutBatchRequest>(
            $"/v1/payouts/estimate-credit",
            finalRequestOptions
        );
    }
}