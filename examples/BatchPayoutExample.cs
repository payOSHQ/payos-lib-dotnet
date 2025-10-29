using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using PayOS.Models.V1.Payouts;
using PayOS.Models.V1.Payouts.Batch;

namespace PayOS.Examples;

public class BatchPayoutExample
{
    public static async Task Run()
    {
        var payosConfig = ConfigurationHelper.GetConfigurationSection("PayOSPayout");

        var client = new PayOSClient(new PayOSOptions
        {
            ClientId = payosConfig["ClientId"],
            ApiKey = payosConfig["ApiKey"],
            ChecksumKey = payosConfig["ChecksumKey"],
            LogLevel = LogLevel.Information
        });

        try
        {
            var referenceId = $"payout_{DateTimeOffset.Now.ToUnixTimeMilliseconds()}";

            var payoutBatchRequest = new PayoutBatchRequest
            {
                ReferenceId = referenceId,
                ValidateDestination = true,
                Category = ["salary"],
                Payouts =
                [
                    new PayoutBatchItem
                    {
                        ReferenceId = $"{referenceId}_0",
                        Amount = 2000,
                        Description = "batch payout",
                        ToBin = "970422",
                        ToAccountNumber = "0123456789"
                    },
                    new PayoutBatchItem
                    {
                        ReferenceId = $"{referenceId}_1",
                        Amount = 2000,
                        Description = "batch payout",
                        ToBin = "970422",
                        ToAccountNumber = "0123456789"
                    }
                ]
            };

            var payoutBatch = await client.Payouts.Batch.CreateAsync(payoutBatchRequest);

            var page = await client.Payouts.ListAsync(new GetPayoutListParam { Limit = 10 });
            Console.WriteLine($"Current page has {page.Data.Count} items");
            Console.WriteLine($"Total items: {page.Pagination.Total}");
            Console.WriteLine($"Has more pages: {page.HasNextPage}");
            Console.WriteLine($"Has previous pages: {page.HasPreviousPage}");

            if (page.HasNextPage)
            {
                var nextPage = await page.GetNextPageAsync();
                Console.WriteLine($"Next page has {nextPage.Data.Count} items");

                if (nextPage.HasPreviousPage)
                {
                    var prevPage = await nextPage.GetPreviousPageAsync();
                    Console.WriteLine($"Previous page has {prevPage.Data.Count} items (back to first page)");
                }
            }

            int count = 0;
            var filterParams = new GetPayoutListParam
            {
                ApprovalState = PayoutApprovalState.Completed,
                Limit = 50
            };

            var completedPage = await client.Payouts.ListAsync(filterParams);
            await foreach (var completedPayout in completedPage)
            {
                count++;
                Console.WriteLine($"Completed Payout {count}: {completedPayout.Id} - {completedPayout.CreatedAt}");
            }
            var accountInfo = await client.PayoutsAccount.GetBalanceAsync();
            Console.WriteLine($"Balance: {accountInfo.Balance}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred: {ex.Message}");
            throw;
        }
    }
}