using System.Collections.Generic;
using System.Net.Http;

using Microsoft.Extensions.Logging;

namespace PayOS;


/// <summary>
/// Configuration options for PayOS client
/// </summary>
public class PayOSOptions
{
    /// <summary>
    /// Client ID for PayOS API authentication
    /// Defaults to environment variable PAYOS_CLIENT_ID
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// API Key for PayOS API authentication
    /// Defaults to environment variable PAYOS_API_KEY
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Checksum Key for signature verification
    /// Defaults to environment variable PAYOS_CHECKSUM_KEY
    /// </summary>
    public string? ChecksumKey { get; set; }

    /// <summary>
    /// Partner Code for PayOS integration
    /// Defaults to environment variable PAYOS_PARTNER_CODE
    /// </summary>
    public string? PartnerCode { get; set; }

    /// <summary>
    /// Base URL for PayOS API
    /// Defaults to https://api-merchant.payos.vn
    /// </summary>
    public string BaseUrl { get; set; } = "https://api-merchant.payos.vn";

    /// <summary>
    /// Request timeout in milliseconds
    /// Defaults to 60000ms (60 seconds)
    /// </summary>
    public int TimeoutMs { get; set; } = 60000;

    /// <summary>
    /// Maximum retry attempts for failed requests
    /// Defaults to 2
    /// </summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>
    /// Logger instance for logging operations
    /// If not provided, a default logger will be created based on LogLevel setting
    /// </summary>
    public ILogger? Logger { get; set; }

    /// <summary>
    /// Log level for the PayOS client
    /// Can be overridden by PAYOS_LOG environment variable
    /// Supported values: Trace, Debug, Information, Warning, Error, Critical, None
    /// Defaults to Information for better debugging experience
    /// </summary>
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Custom HttpClient instance
    /// If not provided, a default HttpClient will be created
    /// </summary>
    public HttpClient? HttpClient { get; set; }

    /// <summary>
    /// Additional headers to include in all requests
    /// </summary>
    public Dictionary<string, string> DefaultHeaders { get; set; } = new();
}