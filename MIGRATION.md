# Migration guide

This guide outlines the changes and steps needed to migrate your codebase to the latest version of the payOS .NET SDK.

## Breaking changes

### Initialize client

```csharp
// before
using Net.payOS;

PayOS client = new PayOS(clientId, apiKey, checksumKey, partnerCode);

// after
using PayOS;

PayOSClient client = new PayOSClient(clientId, apiKey, checksumKey, partnerCode);
```

### Methods name and types

All methods related to payment request now under `PayOSClient.PaymentRequests`.

```csharp
// before
using Net.payOS;
using Net.payOS.Types;

long orderCode = DateTimeOffset.Now.ToUnixTimeMilliseconds();
ItemData item = new ItemData("Mì tôm hảo hảo ly", 1, 1000);
List<ItemData> items = new List<ItemData>();
items.Add(item);
PaymentData paymentData = new PaymentData(orderCode, 1000, "Thanh toan don hang", items, "https://your-url.com/cancel", "https://your-url.com/success");

CreatePaymentResult createPayment = await client.createPaymentLink(paymentData);

// after
using PayOS;
using PayOS.Models.V2.PaymentRequests;

long orderCode = DateTimeOffset.Now.ToUnixTimeMilliseconds();
List<PaymentLinkItem> items = [new PaymentLinkItem
{
    Name = "my tom",
    Price = 1000,
    Quantity = 2
}];
CreatePaymentLinkRequest paymentData = new CreatePaymentLinkRequest
{
    OrderCode = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
    Amount = 2000,
    Description = "payment",
    ReturnUrl = "https://your-url.com/success",
    CancelUrl = "https://your-url.com/cancel",
    Items = items
};
CreatePaymentLinkResponse createPaymentLinkResponse = await client.PaymentRequests.CreateAsync(paymentData);
```

```csharp
// before
using Net.payOS;
using Net.payOS.Types;

PaymentLinkInformation paymentLinkInformation = await client.getPaymentLinkInformation(orderCode);

// after
using PayOS;
using PayOS.Models.V2.PaymentRequests;

PaymentLink paymentLink = await client.PaymentRequests.GetAsync(orderCode);
```

```csharp
// before
using Net.payOS;
using Net.payOS.Types;

long orderCode = 123;
String cancellationReason = "reason";

PaymentLinkInformation cancelledPaymentLinkInfo = client.cancelPaymentLink(orderCode, cancellationReason);

// after
using PayOS;
using PayOS.Models.V2.PaymentRequests;

long orderCode = 123;
string cancellationReason = "reason";

PaymentLink paymentLink = await client.PaymentRequests.CancelAsync(orderCode, cancellationReason);
```

For webhook related methods, they now under `PayOSClient.Webhooks`.

```csharp
// before
using Net.payOS;

string confirmResult = await client.confirmWebhook(webhookUrl);

// after
using PayOS;
using PayOS.Models.Webhooks;

ConfirmWebhookResponse confirmResult = await client.Webhooks.ConfirmAsync(webhookUrl);
```

```csharp
// before
using Net.payOS;
using Net.payOS.Types;

WebhookData webhookData = client.verifyPaymentWebhookData(webhookBody);

// after
using PayOS;
using PayOS.Models.Webhooks;

WebhookData webhookData = await client.Webhooks.VerifyAsync(webhookBody);
```

### Handling errors

API errors are now handled using custom exception classes. You can catch specific exceptions to handle different error scenarios.

```csharp
using PayOS;
using PayOS.Exceptions;

try
{
    // Your code that calls payOS SDK methods
}
catch (ApiException apiException)
{
    // Handle API errors
}
catch (WebhookException webhookException)
{
    // Handle webhook errors
}
catch (InvalidSignatureException signatureException)
{
    // Handle signature errors
}
catch (PayOSException payosException)
{
    // Handle general payOS errors
} catch (Exception ex)
{
    // Handle other errors
}
```
