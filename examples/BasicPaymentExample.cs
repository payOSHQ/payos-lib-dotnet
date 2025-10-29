using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using PayOS.Models.V2.PaymentRequests;

namespace PayOS.Examples;

public class BasicPaymentExample
{
    public static async Task Run()
    {
        var payosConfig = ConfigurationHelper.GetConfigurationSection("PayOS");

        var clientId = payosConfig["ClientId"] ?? throw new ArgumentNullException("ClientId", "ClientId is required in configuration.");
        var apiKey = payosConfig["ApiKey"] ?? throw new ArgumentNullException("ApiKey", "ApiKey is required in configuration.");
        var checksumKey = payosConfig["ChecksumKey"] ?? throw new ArgumentNullException("ChecksumKey", "ChecksumKey is required in configuration.");

        var client = new PayOSClient(clientId, apiKey, checksumKey);

        try
        {
            List<PaymentLinkItem> items = [new PaymentLinkItem
            {
                Name = "my tom",
                Price = 1000,
                Quantity = 2
            }];
            var paymentData = new CreatePaymentLinkRequest
            {
                OrderCode = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                Amount = 2000,
                Description = "payment",
                ReturnUrl = "https://your-url.com/success",
                CancelUrl = "https://your-url.com/cancel",
                Items = items
            };
            var createPaymentLinkResponse = await client.PaymentRequests.CreateAsync(paymentData);
            Console.WriteLine($"Checkout URL: {createPaymentLinkResponse.CheckoutUrl}");
            var paymentLink = await client.PaymentRequests.GetAsync(createPaymentLinkResponse.PaymentLinkId);
            // paymentLink = await client.PaymentRequests.CancelAsync(createPaymentLinkResponse.PaymentLinkId, "change my mind"); // Uncomment this line for cancel payment link
        }
        catch (Exception)
        {
            throw;
        }
    }
}