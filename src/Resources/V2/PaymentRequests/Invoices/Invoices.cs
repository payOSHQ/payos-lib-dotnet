using System.Threading.Tasks;

using PayOS.Models;
using PayOS.Models.V2.PaymentRequests.Invoices;

namespace PayOS.Resources.V2.PaymentRequests.Invoices;

/// <summary>
/// Invoices resource for managing payment request invoices
/// </summary>
public class Invoices(PayOSClient client) : ApiResource(client)
{
    /// <summary>
    /// Retrieve invoices of a payment link by payment link ID
    /// </summary>
    /// <param name="paymentLinkId">Payment link ID</param>
    /// <param name="requestOptions">Request options</param>
    /// <returns>A promise resolving to an InvoicesInfo object containing invoices of the current payment link</returns>
    public async Task<InvoicesInfo> GetAsync(
        string paymentLinkId,
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
        return await _client.GetAsync<InvoicesInfo>(
            $"/v2/payment-requests/{paymentLinkId}/invoices",
            finalRequestOptions
        );
    }

    /// <summary>
    /// Retrieve invoices of a payment link by order code
    /// </summary>
    /// <param name="orderCode">Order code</param>
    /// <param name="requestOptions">Request options</param>
    /// <returns>A promise resolving to an InvoicesInfo object containing invoices of the current payment link</returns>
    public async Task<InvoicesInfo> GetAsync(
        long orderCode,
        RequestOptions? requestOptions = null)
    {
        return await GetAsync(orderCode.ToString(), requestOptions);
    }

    /// <summary>
    /// Download an invoice in PDF format by payment link ID
    /// </summary>
    /// <param name="invoiceId">Invoice ID</param>
    /// <param name="paymentLinkId">Payment link ID</param>
    /// <param name="requestOptions">Request options</param>
    /// <returns>The invoice file in PDF format</returns>
    public async Task<FileDownloadResponse> DownloadAsync(
        string invoiceId,
        string paymentLinkId,
        RequestOptions? requestOptions = null)
    {
        return await _client.DownloadFileAsync(
            $"/v2/payment-requests/{paymentLinkId}/invoices/{invoiceId}/download",
            requestOptions ?? new RequestOptions()
        );
    }

    /// <summary>
    /// Download an invoice in PDF format by order code
    /// </summary>
    /// <param name="invoiceId">Invoice ID</param>
    /// <param name="orderCode">Order code</param>
    /// <param name="requestOptions">Request options</param>
    /// <returns>The invoice file in PDF format</returns>
    public async Task<FileDownloadResponse> DownloadAsync(
        string invoiceId,
        long orderCode,
        RequestOptions? requestOptions = null)
    {
        return await DownloadAsync(invoiceId, orderCode.ToString(), requestOptions);
    }
}