using System.Collections.Generic;
using System.Threading;

namespace PayOS.Models;

/// <summary>
/// Options for configuring HTTP requests
/// </summary>
public class RequestOptions
{
    /// <summary>
    /// Query parameters to include in the request URL
    /// </summary>
    public Dictionary<string, object>? Query { get; set; }

    /// <summary>
    /// Additional HTTP headers to include with the request
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// The maximum number of times that the client will retry a request
    /// </summary>
    public int? MaxRetries { get; set; }

    /// <summary>
    /// The maximum amount of time (in milliseconds) that the client should wait for a response
    /// from the server before timing out of a request
    /// </summary>
    public int? TimeoutMs { get; set; }

    /// <summary>
    /// Signature configuration for request and response validation
    /// </summary>
    public SignatureOptions? Signature { get; set; }

    /// <summary>
    /// Cancellation token for the request
    /// </summary>
    public CancellationToken CancellationToken { get; set; } = default;
}

/// <summary>
/// Options for configuring HTTP requests with a request body
/// </summary>
/// <typeparam name="T">The type of the request body</typeparam>
public class RequestOptions<T> : RequestOptions
{
    /// <summary>
    /// The request body
    /// </summary>
    public T? Body { get; set; }
}