using System.Threading.Tasks;

using PayOS.Models;
using PayOS.Models.V1.PayoutsAccount;

namespace PayOS.Resources.V1.PayoutsAccount;


/// <summary>
/// Payouts account resource for managing payout account operations
/// </summary>
public class PayoutsAccount(PayOSClient client) : ApiResource(client)
{

    /// <summary>
    /// Retrieves the current payout account balance
    /// </summary>
    /// <param name="requestOptions">Request options</param>
    /// <returns>A promise resolving to a PayoutAccountInfo object containing the current balance and related account details</returns>
    public async Task<PayoutAccountInfo> GetBalanceAsync(RequestOptions? requestOptions = null)
    {
        var finalRequestOptions = new RequestOptions<object>
        {
            Signature = requestOptions?.Signature,
            Headers = requestOptions?.Headers,
            Body = null,
            Query = requestOptions?.Query,
            MaxRetries = requestOptions?.MaxRetries,
            TimeoutMs = requestOptions?.TimeoutMs,
            CancellationToken = requestOptions?.CancellationToken ?? default
        };
        return await _client.GetAsync<PayoutAccountInfo>(
            "/v1/payouts-account/balance",
            finalRequestOptions
        );
    }
}