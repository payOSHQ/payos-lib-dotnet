using System.Threading;
using System.Threading.Tasks;

using PayOS.Models;
using PayOS.Models.V2.PaymentRequests;

using InvoicesResource = PayOS.Resources.V2.PaymentRequests.Invoices.Invoices;

namespace PayOS.Resources.V2.PaymentRequests;


/// <summary>
/// Payment requests resource for managing payment links
/// </summary>
public class PaymentRequests(PayOSClient client) : ApiResource(client)
{
    /// <summary>
    /// Invoices resource for managing payment request invoices
    /// </summary>
    public InvoicesResource Invoices { get; } = new(client);

    /// <summary>
    /// Creates a payment link for the provided order data
    /// </summary>
    /// <param name="paymentData">The details of payment link to be created</param>
    /// <param name="requestOptions">Request options</param>
    /// <returns>A promise resolving to a CreatePaymentLinkResponse object containing the payment link detail</returns>
    public async Task<CreatePaymentLinkResponse> CreateAsync(
        CreatePaymentLinkRequest paymentData,
        RequestOptions<CreatePaymentLinkRequest>? requestOptions = null)
    {
        var signatureOptions = new SignatureOptions
        {
            Request = RequestSignatureTypes.CreatePaymentLink,
            Response = ResponseSignatureTypes.Body
        };
        var finalRequestOptions = new RequestOptions<CreatePaymentLinkRequest>
        {
            Signature = requestOptions?.Signature ?? signatureOptions,
            Headers = requestOptions?.Headers,
            Body = paymentData,
            Query = requestOptions?.Query,
            MaxRetries = requestOptions?.MaxRetries,
            TimeoutMs = requestOptions?.TimeoutMs,
            CancellationToken = requestOptions?.CancellationToken ?? default
        };
        return await _client.PostAsync<CreatePaymentLinkResponse, CreatePaymentLinkRequest>(
            "/v2/payment-requests",
           finalRequestOptions
        );
    }

    /// <summary>
    /// Retrieve payment link information from payment link ID or order code
    /// </summary>
    /// <param name="idOrOrderCode">The payment link ID or order code</param>
    /// <param name="requestOptions">Request options</param>
    /// <returns>A promise resolving to a PaymentLink object containing the payment link detail</returns>
    public async Task<PaymentLink> GetAsync(
        string idOrOrderCode,
        RequestOptions? requestOptions = null)
    {
        var signatureOptions = new SignatureOptions
        {
            Response = ResponseSignatureTypes.Body
        };
        var finalRequestOptions = new RequestOptions<object>
        {
            Signature = requestOptions?.Signature ?? signatureOptions,
            Headers = requestOptions?.Headers,
            Body = null,
            Query = requestOptions?.Query,
            MaxRetries = requestOptions?.MaxRetries,
            TimeoutMs = requestOptions?.TimeoutMs,
            CancellationToken = requestOptions?.CancellationToken ?? default
        };
        return await _client.GetAsync<PaymentLink>(
            $"/v2/payment-requests/{idOrOrderCode}",
            finalRequestOptions
        );
    }

    /// <summary>
    /// Retrieve payment link information from order code
    /// </summary>
    /// <param name="orderCode">The order code</param>
    /// <param name="requestOptions">Request options</param>
    /// <returns>A promise resolving to a PaymentLink object containing the payment link detail</returns>
    public async Task<PaymentLink> GetAsync(
        long orderCode,
        RequestOptions? requestOptions = null)
    {
        return await GetAsync(orderCode.ToString(), requestOptions);
    }

    /// <summary>
    /// Cancel a payment link
    /// </summary>
    /// <param name="idOrOrderCode">The payment link ID or order code</param>
    /// <param name="cancellationReason">Reason for cancellation (optional)</param>
    /// <param name="requestOptions">Request options</param>
    /// <returns>A promise resolving to a PaymentLink object</returns>
    public async Task<PaymentLink> CancelAsync(
        string idOrOrderCode,
        string? cancellationReason = null,
        RequestOptions<CancelPaymentLinkRequest>? requestOptions = null)
    {
        var body = new CancelPaymentLinkRequest
        {
            CancellationReason = cancellationReason
        };

        var signatureOptions = new SignatureOptions
        {
            Response = ResponseSignatureTypes.Body
        };
        var finalRequestOptions = new RequestOptions<CancelPaymentLinkRequest>
        {
            Signature = requestOptions?.Signature ?? signatureOptions,
            Headers = requestOptions?.Headers,
            Body = body,
            Query = requestOptions?.Query,
            MaxRetries = requestOptions?.MaxRetries,
            TimeoutMs = requestOptions?.TimeoutMs,
            CancellationToken = requestOptions?.CancellationToken ?? default
        };
        return await _client.PostAsync<PaymentLink, CancelPaymentLinkRequest>(
            $"/v2/payment-requests/{idOrOrderCode}/cancel",
            finalRequestOptions
        );
    }

    /// <summary>
    /// Cancel a payment link by order code
    /// </summary>
    /// <param name="orderCode">The order code</param>
    /// <param name="cancellationReason">Reason for cancellation (optional)</param>
    /// <param name="requestOptions">Request options</param>
    /// <returns>A promise resolving to a PaymentLink object</returns>
    public async Task<PaymentLink> CancelAsync(
        long orderCode,
        string? cancellationReason = null,
        RequestOptions<CancelPaymentLinkRequest>? requestOptions = null)
    {
        return await CancelAsync(orderCode.ToString(), cancellationReason, requestOptions);
    }
}