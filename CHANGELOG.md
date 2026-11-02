# Changelog

## 2.1.1 (2026-10-30)

### Bug Fixes

- **webhooks:** add VerifyAsync(string) raw-body overload with strict data validation; value checks on typed VerifyAsync ([GHSA-2wmq-932c-mf92](https://github.com/payOSHQ/payos-lib-dotnet/security/advisories/GHSA-2wmq-932c-mf92))
- **client:** fail closed when a signed response is missing its signature; constant-time comparison ([GHSA-pmp4-56f8-xwjc](https://github.com/payOSHQ/payos-lib-dotnet/security/advisories/GHSA-pmp4-56f8-xwjc))
- **client:** percent-encode id/orderCode/invoiceId path segments and reject empty, dot-segment, negative and unsafe-integer values; default HttpClient follows only 301/302/303/307/308 redirects (max 5) and stops sending `x-api-key`/`x-client-id`/`x-partner-code`/`authorization`/`cookie`/`proxy-authorization` headers and stored cookies once a redirect leaves the API origin; validate body-signed response data keys and values before signature verification

## 2.1.0 (2026-03-18)

### Features

- **payment-requests**: validate `orderCode` against the SDK max safe number before signing or calling payment request and invoice endpoints

### Refactoring

- **invoices**: remove the unused `requestOptions` parameter from `SaveToFileAsync` and `SaveToDirectoryAsync`

### Documentation

- **readme**: correct the examples link path

## 2.0.1 (2025-10-29)

### Documentation

- **documentation**: update missing documentation file

## 2.0.0 (2025-10-29)

### Features

- **api:** add `/v2/payment-request/invoices`
- **api:** add `/v1/payouts`
- **api:** add `/v1/payouts-account`
- **client:** add default value for credential read from environment variable
- **client:** add `PayOS.Crypto` to calculate signature for payment-requests and payouts signature
- **client:** add additional options to all method
- **client:** add logging with custom logger
- **client:** add pagination support for get list request
- **client:** add retry for rate limit request
- **client:** add Error subclass to handle api error, webhook error and signature error for better error handling
- **client:** add support for request download file

### Documentation

- **readme:** update readme
