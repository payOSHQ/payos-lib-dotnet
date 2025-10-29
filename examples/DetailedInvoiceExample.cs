using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using PayOS.Exceptions;
using PayOS.Models.V2.PaymentRequests;

namespace PayOS.Examples;

public class DetailedInvoiceExample
{
    public static async Task Run()
    {
        var payosConfig = ConfigurationHelper.GetConfigurationSection("PayOS");

        var client = new PayOSClient(new PayOSOptions
        {
            ClientId = payosConfig["ClientId"],
            ApiKey = payosConfig["ApiKey"],
            ChecksumKey = payosConfig["ChecksumKey"],
            LogLevel = LogLevel.Information
        });

        try
        {
            var orderCode = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            var paymentData = new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = 126500, // Total amount including tax
                Description = $"order{orderCode}",
                ReturnUrl = "https://your-website.com/payment/success",
                CancelUrl = "https://your-website.com/payment/cancel",

                // Buyer information
                BuyerName = "Nguyen Van A",
                BuyerEmail = "customer@example.com",
                BuyerPhone = "012456789",
                BuyerAddress = "123 Nguyen Trai, District 1, Ho Chi Minh City",
                BuyerCompanyName = "ABC Company Ltd.",
                BuyerTaxCode = "0123456789-001",

                // Detailed items breakdown
                Items =
                [
                    new()
                    {
                        Name = "iPhone 15 Pro Case",
                        Quantity = 2,
                        Price = 25000,
                        Unit = "piece",
                        TaxPercentage = (TaxPercentage)10
                    },
                    new()
                    {
                        Name = "Screen Protector",
                        Quantity = 1,
                        Price = 15000,
                        Unit = "piece",
                        TaxPercentage = (TaxPercentage)10
                    },
                    new()
                    {
                        Name = "Wireless Charger",
                        Quantity = 1,
                        Price = 50000,
                        Unit = "piece",
                        TaxPercentage = (TaxPercentage)10
                    }
                ],

                // Invoice configuration
                Invoice = new InvoiceRequest
                {
                    BuyerNotGetInvoice = false, // Customer wants invoice
                    TaxPercentage = (TaxPercentage)10 // Overall tax percentage
                },

                ExpiredAt = DateTimeOffset.Now.ToUnixTimeSeconds() + 60 * 60 // Expired in 1 hour
            };

            var paymentLink = await client.PaymentRequests.CreateAsync(paymentData);

            Console.WriteLine($"Checkout URL: {paymentLink.CheckoutUrl}");
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"Failed to create payment link: {ex.StatusCode} - {ex.ErrorCode} - {ex.ErrorDescription}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }
    }
}