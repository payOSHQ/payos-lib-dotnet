using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using PayOS.Crypto;
using PayOS.Exceptions;
using PayOS.Internal;
using PayOS.Models;
using PayOS.Models.V2.PaymentRequests.Invoices;
using PayOS.Resources.V1.Payouts;
using PayOS.Resources.V1.PayoutsAccount;
using PayOS.Resources.V2.PaymentRequests;
using PayOS.Resources.Webhooks;

namespace PayOS;


/// <summary>
/// PayOS API client for interacting with payOS Merchant API
/// </summary>
public class PayOSClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _disposeHttpClient;
    private readonly ILogger? _logger;
    private readonly PayOSOptions _options;

    public string ClientId { get; }
    public string ApiKey { get; }
    public string ChecksumKey { get; }
    public string? PartnerCode { get; }
    public string BaseUrl { get; }
    public CryptoProvider Crypto { get; }

    // Resources
    public PaymentRequests PaymentRequests { get; }
    public Webhooks Webhooks { get; }
    public Payouts Payouts { get; }
    public PayoutsAccount PayoutsAccount { get; }

    // Constants
    public const int DefaultTimeout = 60000;
    public const int DefaultMaxRetries = 2;

    /// <summary>
    /// Create a new PayOS API client instance
    /// </summary>
    /// <param name="options">Client configuration options</param>
    public PayOSClient(PayOSOptions? options = null)
    {
        _options = options ?? new PayOSOptions();

        // Initialize credentials from options or environment variables
        ClientId = _options.ClientId ?? Environment.GetEnvironmentVariable("PAYOS_CLIENT_ID")
            ?? throw new PayOSException("The PAYOS_CLIENT_ID environment variable is missing or empty; either provide it, or instantiate the PayOS client with a ClientId option.");

        ApiKey = _options.ApiKey ?? Environment.GetEnvironmentVariable("PAYOS_API_KEY")
            ?? throw new PayOSException("The PAYOS_API_KEY environment variable is missing or empty; either provide it, or instantiate the PayOS client with an ApiKey option.");

        ChecksumKey = _options.ChecksumKey ?? Environment.GetEnvironmentVariable("PAYOS_CHECKSUM_KEY")
            ?? throw new PayOSException("The PAYOS_CHECKSUM_KEY environment variable is missing or empty; either provide it, or instantiate the PayOS client with a ChecksumKey option.");

        PartnerCode = _options.PartnerCode ?? Environment.GetEnvironmentVariable("PAYOS_PARTNER_CODE");

        BaseUrl = _options.BaseUrl ?? Environment.GetEnvironmentVariable("PAYOS_BASE_URL") ?? "https://api-merchant.payos.vn";

        // Initialize logger with default configuration if not provided
        _logger = _options.Logger ?? CreateDefaultLogger();

        // Initialize HttpClient
        if (_options.HttpClient != null)
        {
            _httpClient = _options.HttpClient;
            _disposeHttpClient = false;
        }
        else
        {
            _httpClient = new HttpClient();
            _disposeHttpClient = true;
        }

        ConfigureHttpClient();

        // Initialize crypto provider
        Crypto = new CryptoProvider();

        // Initialize resources
        PaymentRequests = new PaymentRequests(this);
        Webhooks = new Webhooks(this);
        Payouts = new Payouts(this);
        PayoutsAccount = new PayoutsAccount(this);
    }

    public PayOSClient(string clientId, string apiKey, string checksumKey, string? partnerCode = null) : this(new PayOSOptions
    {
        ClientId = clientId,
        ApiKey = apiKey,
        ChecksumKey = checksumKey,
        PartnerCode = partnerCode
    })
    {
    }

    /// <summary>
    /// Create a default logger with configured log level
    /// </summary>
    private ILogger? CreateDefaultLogger()
    {
        var logLevel = _options.LogLevel;

        var envLogLevel = Environment.GetEnvironmentVariable("PAYOS_LOG");
        if (!string.IsNullOrEmpty(envLogLevel) && Enum.TryParse<LogLevel>(envLogLevel, true, out var parsedLevel))
        {
            logLevel = parsedLevel;
        }

        return logLevel == LogLevel.None ? null : (ILogger)new Logger("PayOS", logLevel);
    }

    /// <summary>
    /// Configure the HttpClient with default settings
    /// </summary>
    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(BaseUrl);
        _httpClient.Timeout = TimeSpan.FromMilliseconds(_options.TimeoutMs);

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("x-client-id", ClientId);
        _httpClient.DefaultRequestHeaders.Add("x-api-key", ApiKey);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", GetUserAgent());

        if (!string.IsNullOrEmpty(PartnerCode))
        {
            _httpClient.DefaultRequestHeaders.Add("x-partner-code", PartnerCode);
        }

        foreach (var header in _options.DefaultHeaders)
        {
            _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
        }
    }

    /// <summary>
    /// Get user agent string
    /// </summary>
    private static string GetUserAgent()
    {
        var version = typeof(PayOSClient).Assembly.GetName().Version?.ToString() ?? "unknown";
        return $"PayOS.Net/{version}";
    }

    /// <summary>
    /// Make a GET request to the API using RequestOptions
    /// </summary>
    public async Task<T> GetAsync<T>(string path, RequestOptions<object> options)
    {
        return await RequestAsync<T, object>(HttpMethod.Get, path, options);
    }

    /// <summary>
    /// Make a POST request to the API using RequestOptions
    /// </summary>
    public async Task<T> PostAsync<T, TRequest>(string path, RequestOptions<TRequest> options)
    {
        return await RequestAsync<T, TRequest>(HttpMethod.Post, path, options);
    }

    /// <summary>
    /// Make a PUT request to the API using RequestOptions
    /// </summary>
    public async Task<T> PutAsync<T, TRequest>(string path, RequestOptions<TRequest> options)
    {
        return await RequestAsync<T, TRequest>(HttpMethod.Put, path, options);
    }

    /// <summary>
    /// Make a DELETE request to the API using RequestOptions
    /// </summary>
    public async Task<T> DeleteAsync<T, TRequest>(string path, RequestOptions<TRequest> options)
    {
        return await RequestAsync<T, TRequest>(HttpMethod.Delete, path, options);
    }

    /// <summary>
    /// Download a file from the API
    /// </summary>
    public async Task<FileDownloadResponse> DownloadFileAsync(string path, RequestOptions requestOptions)
    {
        var url = BuildUrl(path, requestOptions.Query);
        var requestId = GenerateRequestId();

        try
        {
            _logger?.LogDebug("[{RequestId}] Making file download request to {Path}", requestId, path);

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, requestOptions.CancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var headers = response.Headers.ToDictionary(h => h.Key, h => (object)string.Join(", ", h.Value));
                var exception = response.StatusCode switch
                {
                    HttpStatusCode.BadRequest => new BadRequestException(headers: headers),
                    HttpStatusCode.Unauthorized => new UnauthorizedException(headers: headers),
                    HttpStatusCode.Forbidden => new ForbiddenException(headers: headers),
                    HttpStatusCode.NotFound => new NotFoundException(headers: headers),
                    (HttpStatusCode)429 => new TooManyRequestsException(headers: headers),
                    _ when ((int)response.StatusCode) >= 500 => new InternalServerErrorException((int)response.StatusCode, headers: headers),
                    _ => new ApiException((int)response.StatusCode, headers: headers)
                };
                throw exception;
            }

            var contentType = response.Content.Headers.ContentType?.MediaType;
            var fileName = ExtractFileNameFromResponse(response);
            var contentLength = response.Content.Headers.ContentLength;

            // Copy content to a new MemoryStream to avoid disposal issues
            var memoryStream = new MemoryStream();
            await response.Content.CopyToAsync(memoryStream);
            memoryStream.Position = 0; // Reset position to beginning

            _logger?.LogDebug("[{RequestId}] File download succeeded", requestId);

            return new FileDownloadResponse
            {
                Content = memoryStream,
                ContentType = contentType,
                FileName = fileName,
                ContentLength = contentLength
            };
        }
        catch (TaskCanceledException) when (requestOptions.CancellationToken.IsCancellationRequested)
        {
            throw new UserAbortException();
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            throw new ConnectionTimeoutException();
        }
        catch (HttpRequestException ex)
        {
            throw new ConnectionException("Connection error occurred", ex);
        }
    }

    /// <summary>
    /// Extract filename from response headers
    /// </summary>
    private static string? ExtractFileNameFromResponse(HttpResponseMessage response)
    {
        var contentDisposition = response.Content.Headers.ContentDisposition;
        return contentDisposition?.FileName != null
            ? contentDisposition.FileName.Trim('"')
            : contentDisposition?.FileNameStar != null ? contentDisposition.FileNameStar : null;
    }

    /// <summary>
    /// Make a request to the API with retry logic
    /// </summary>
    internal async Task<T> RequestAsync<T, TBody>(HttpMethod method, string path, RequestOptions<TBody> requestOptions)
    {
        var retriesRemaining = requestOptions.MaxRetries ?? _options.MaxRetries;
        var url = BuildUrl(path, requestOptions.Query);
        var requestId = GenerateRequestId();
        var originalRequestId = requestId;

        while (true)
        {
            try
            {
                _logger?.LogDebug("[{RequestId}] Making {Method} request to {Path}. Retries remaining: {Retries}",
                    requestId, method, path, retriesRemaining);

                using var request = new HttpRequestMessage(method, url);

                if (requestOptions.Body != null)
                {
                    var (jsonBody, requestHeaders) = ProcessRequestBody(requestOptions.Body, requestOptions.Signature);
                    request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                    if (requestHeaders != null)
                    {
                        foreach (var header in requestHeaders)
                        {
                            request.Headers.Add(header.Key, header.Value);
                        }
                    }
                }

                // Add additional headers if provided
                if (requestOptions.Headers != null)
                {
                    foreach (var header in requestOptions.Headers)
                    {
                        request.Headers.Add(header.Key, header.Value);
                    }
                }

                using var response = await _httpClient.SendAsync(request, requestOptions.CancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger?.LogDebug("[{RequestId}] Request succeeded with status {StatusCode}",
                        requestId, response.StatusCode);
                    _logger?.LogDebug("[{RequestId}] Received response: {StatusCode} {Content}", requestId, response.StatusCode, await response.Content.ReadAsStringAsync());
                    return await ProcessResponseAsync<T>(response, requestOptions.Signature);
                }

                var shouldRetry = ShouldRetryRequest(response.StatusCode) && retriesRemaining > 0;

                if (shouldRetry)
                {
                    retriesRemaining--;
                    var delay = await CalculateRetryDelayAsync(response, requestOptions.MaxRetries ?? _options.MaxRetries, retriesRemaining);

                    _logger?.LogWarning("[{RequestId}] Request failed with status {StatusCode}, retrying in {Delay}ms. Retries remaining: {Retries}",
                        requestId, response.StatusCode, delay.TotalMilliseconds, retriesRemaining);

                    await Task.Delay(delay, requestOptions.CancellationToken);
                    requestId = GenerateRequestId() + $" (retry of {originalRequestId})";
                    continue;
                }

                _logger?.LogWarning("[{RequestId}] Request failed with status {StatusCode}, no retries {Reason}",
                    requestId, response.StatusCode, shouldRetry ? "remaining" : "allowed for this status");
                _logger?.LogDebug("[{RequestId}] Received response: {StatusCode} {Content}", requestId, response.StatusCode, await response.Content.ReadAsStringAsync());
                return await ProcessResponseAsync<T>(response, requestOptions.Signature);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException && retriesRemaining > 0)
            {
                retriesRemaining--;
                var delay = await CalculateRetryDelayAsync(null, requestOptions.MaxRetries ?? _options.MaxRetries, retriesRemaining);

                _logger?.LogWarning("[{RequestId}] Request timeout, retrying in {Delay}ms. Retries remaining: {Retries}",
                    requestId, delay.TotalMilliseconds, retriesRemaining);

                await Task.Delay(delay, requestOptions.CancellationToken);
                requestId = GenerateRequestId() + $" (retry of {originalRequestId})";
                continue;
            }
            catch (TaskCanceledException) when (requestOptions.CancellationToken.IsCancellationRequested)
            {
                throw new UserAbortException();
            }
            catch (HttpRequestException ex) when (retriesRemaining > 0)
            {
                retriesRemaining--;
                var delay = await CalculateRetryDelayAsync(null, requestOptions.MaxRetries ?? _options.MaxRetries, retriesRemaining);

                _logger?.LogWarning("[{RequestId}] Connection error: {Error}, retrying in {Delay}ms. Retries remaining: {Retries}",
                    requestId, ex.Message, delay.TotalMilliseconds, retriesRemaining);

                await Task.Delay(delay, requestOptions.CancellationToken);
                requestId = GenerateRequestId() + $" (retry of {originalRequestId})";
                continue;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                throw new ConnectionTimeoutException();
            }
            catch (HttpRequestException ex)
            {
                throw new ConnectionException("Connection error occurred", ex);
            }
        }
    }

    /// <summary>
    /// Process request body and add signature if needed
    /// </summary>
    private (string jsonBody, Dictionary<string, string>? headers) ProcessRequestBody(object body, SignatureOptions? signatureOptions)
    {
        Dictionary<string, string>? headers = null;

        if (signatureOptions?.Request != null)
        {
            var signature = signatureOptions.Request switch
            {
                RequestSignatureTypes.CreatePaymentLink => Crypto.CreateSignatureOfPaymentRequest(body, ChecksumKey),
                RequestSignatureTypes.Body => Crypto.CreateSignatureFromObject(body, ChecksumKey),
                RequestSignatureTypes.Header => Crypto.CreateSignature(ChecksumKey, body),
                _ => throw new InvalidSignatureException($"Invalid signature type: {signatureOptions.Request}")
            } ?? throw new InvalidSignatureException("Failed to create signature");

            if (signatureOptions.Request is RequestSignatureTypes.CreatePaymentLink or RequestSignatureTypes.Body)
            {
                var bodyWithSignature = JsonSerializer.SerializeToElement(body);
                var dictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(bodyWithSignature.GetRawText()) ?? [];
                dictionary["signature"] = signature;
                return (JsonSerializer.Serialize(dictionary), null);
            }
            else if (signatureOptions.Request == RequestSignatureTypes.Header)
            {
                headers = new Dictionary<string, string> { { "x-signature", signature } };
            }
        }

        return (JsonSerializer.Serialize(body), headers);
    }

    /// <summary>
    /// Process HTTP response and handle errors
    /// </summary>
    private async Task<T> ProcessResponseAsync<T>(HttpResponseMessage response, SignatureOptions? signatureOptions = null)
    {
        var responseContent = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            var headers = response.Headers.ToDictionary(h => h.Key, h => (object)string.Join(", ", h.Value));

            var exception = response.StatusCode switch
            {
                HttpStatusCode.BadRequest => new BadRequestException(headers: headers),
                HttpStatusCode.Unauthorized => new UnauthorizedException(headers: headers),
                HttpStatusCode.Forbidden => new ForbiddenException(headers: headers),
                HttpStatusCode.NotFound => new NotFoundException(headers: headers),
                (HttpStatusCode)429 => new TooManyRequestsException(headers: headers), // TooManyRequests not available in .NET Standard 2.0
                _ when ((int)response.StatusCode) >= 500 => new InternalServerErrorException((int)response.StatusCode, headers: headers),
                _ => new ApiException((int)response.StatusCode, headers: headers)
            };

            throw exception;
        }

        try
        {
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<T>>(responseContent);

            if (apiResponse?.Code != "00" || apiResponse.Data == null)
            {
                throw new ApiException(
                    statusCode: (int)response.StatusCode,
                    errorCode: apiResponse?.Code,
                    errorDescription: apiResponse?.Desc ?? "Unknown error"
                );
            }

            if (signatureOptions?.Response != null && apiResponse.Data != null)
            {
                VerifyResponseSignature(apiResponse, response, signatureOptions.Response);
            }

            return apiResponse.Data;
        }
        catch (JsonException ex)
        {
            throw new ApiException(errorDescription: $"Failed to deserialize response: {ex.Message}");
        }
    }

    /// <summary>
    /// Verify response signature
    /// </summary>
    private void VerifyResponseSignature<T>(ApiResponse<T> apiResponse, HttpResponseMessage httpResponse, string responseSignatureType)
    {
        string? responseSignature = responseSignatureType switch
        {
            ResponseSignatureTypes.Body => apiResponse.Signature,
            ResponseSignatureTypes.Header => httpResponse.Headers.GetValues("x-signature")?.FirstOrDefault(),
            _ => throw new InvalidSignatureException($"Invalid response signature type: {responseSignatureType}")
        };

        if (!string.IsNullOrEmpty(responseSignature) && apiResponse.Data != null)
        {
            var expectedSignature = responseSignatureType switch
            {
                ResponseSignatureTypes.Body => Crypto.CreateSignatureFromObject(apiResponse.Data, ChecksumKey),
                ResponseSignatureTypes.Header => Crypto.CreateSignature(ChecksumKey, apiResponse.Data),
                _ => throw new InvalidSignatureException($"Invalid response signature type: {responseSignatureType}")
            };

            if (responseSignature != expectedSignature)
            {
                throw new InvalidSignatureException("Data integrity check failed");
            }
        }
    }

    /// <summary>
    /// Determine if a request should be retried based on HTTP status code
    /// </summary>
    private static bool ShouldRetryRequest(HttpStatusCode statusCode)
    {
        return statusCode == HttpStatusCode.RequestTimeout ||
               statusCode == (HttpStatusCode)429 || // TooManyRequests
               (int)statusCode >= 500;
    }

    /// <summary>
    /// Calculate retry delay based on response headers and attempt number
    /// </summary>
    private static async Task<TimeSpan> CalculateRetryDelayAsync(HttpResponseMessage? response, int maxRetries, int retriesRemaining)
    {
        var timeoutMs = (double?)null;

        // Check for Retry-After header (RFC 7231)
        if (response?.Headers.RetryAfter != null)
        {
            if (response.Headers.RetryAfter.Delta.HasValue)
            {
                timeoutMs = response.Headers.RetryAfter.Delta.Value.TotalMilliseconds;
            }
            else if (response.Headers.RetryAfter.Date.HasValue)
            {
                timeoutMs = (response.Headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow).TotalMilliseconds;
            }
        }

        // Check for x-ratelimit-reset header
        if (response?.Headers.TryGetValues("x-ratelimit-reset", out var rateLimitValues) == true)
        {
            var rateLimitReset = rateLimitValues.FirstOrDefault();
            if (rateLimitReset != null && double.TryParse(rateLimitReset, out var resetSeconds))
            {
                timeoutMs = (resetSeconds * 1000) - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
        }

        // If no valid header-based delay or delay is too long/negative, use exponential backoff
        if (!(timeoutMs.HasValue && timeoutMs >= 0 && timeoutMs < 60 * 1000))
        {
            const double initRetryDelay = 0.5; // 500ms
            const double maxRetryDelay = 10.0; // 10 seconds
            var numRetries = maxRetries - retriesRemaining;
            var sleepSeconds = Math.Min(initRetryDelay * Math.Pow(2, numRetries), maxRetryDelay);

            // Apply jitter to avoid thunder herd (25% jitter)
            var jitter = 1 - (new Random().NextDouble() * 0.25);
            timeoutMs = sleepSeconds * jitter * 1000;
        }

        return await Task.FromResult(TimeSpan.FromMilliseconds(Math.Max(0, timeoutMs.Value)));
    }

    /// <summary>
    /// Generate a unique request ID for logging correlation
    /// </summary>
    private static string GenerateRequestId()
    {
        var random = new Random();
        var randomValue = random.Next(0, 1 << 24);
        return $"log_{randomValue:x6}";
    }

    /// <summary>
    /// Build URL with query parameters
    /// </summary>
    private static string BuildUrl(string path, Dictionary<string, object>? queryParams)
    {
        var url = path.StartsWith("/") ? path : $"/{path}";

        if (queryParams != null && queryParams.Count != 0)
        {
            var queryString = string.Join("&", queryParams.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value?.ToString() ?? "")}"));
            url += $"?{queryString}";
        }

        return url;
    }

    /// <summary>
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        if (_disposeHttpClient)
        {
            _httpClient?.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}