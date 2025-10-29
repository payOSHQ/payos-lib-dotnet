using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using PayOS.Exceptions;
using PayOS.Models.Webhooks;

namespace PayOS.Examples;

public class WebhookHandlingExample
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

        // Mock webhook data for demonstration
        // In real usage, this would come from payOS webhook POST request
        var mockWebhookData = new Webhook
        {
            Code = "00",
            Description = "success",
            Success = true,
            Data = new WebhookData
            {
                OrderCode = 123,
                Amount = 3000,
                Description = "VQRIO123",
                AccountNumber = "12345678",
                Reference = "TF230204212323",
                TransactionDateTime = "2023-02-04 18:25:00",
                Currency = "VND",
                PaymentLinkId = "124c33293c43417ab7879e14c8d9eb18",
                Code = "00",
                Description2 = "Thành công"
            },
            Signature = ""
        };

        // Calculate the webhook signature from mock data, this signature will be sent by payOS
        // You can using this method to manually verify the webhook data
        mockWebhookData.Signature = client.Crypto.CreateSignatureFromObject(mockWebhookData.Data, client.ChecksumKey) ?? "";

        try
        {
            var webhookUrl = "https://your-domain.com/payos-webhook";
            var confirmResult = await client.Webhooks.ConfirmAsync(webhookUrl);
            Console.WriteLine($"Webhook URL confirmed: {confirmResult.WebhookUrl}");
        }
        catch (WebhookException ex)
        {
            Console.WriteLine($"Webhook registration failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }

        try
        {
            var verifiedData = await client.Webhooks.VerifyAsync(mockWebhookData);
            Console.WriteLine(verifiedData);

            // Process the payment confirmation...
        }
        catch (WebhookException ex)
        {
            Console.WriteLine($"Webhook verification failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }
    }
}