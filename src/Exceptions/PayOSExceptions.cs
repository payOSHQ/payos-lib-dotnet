using System;
using System.Collections.Generic;

namespace PayOS.Exceptions;


/// <summary>
/// Base exception class for all PayOS-related errors
/// </summary>
public class PayOSException : Exception
{
    public PayOSException() { }
    public PayOSException(string message) : base(message) { }
    public PayOSException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when API requests fail
/// </summary>
public class ApiException : PayOSException
{
    public int? StatusCode { get; }
    public string? ErrorCode { get; }
    public string? ErrorDescription { get; }
    public Dictionary<string, object>? Headers { get; }

    public ApiException(int? statusCode = null, string? errorCode = null, string? errorDescription = null, Dictionary<string, object>? headers = null)
        : base(errorDescription ?? "API request failed")
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        ErrorDescription = errorDescription;
        Headers = headers;
    }

    public ApiException(int? statusCode, string? errorCode, string? errorDescription, Exception innerException)
        : base(errorDescription ?? "API request failed", innerException)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        ErrorDescription = errorDescription;
    }
}

/// <summary>
/// Exception thrown when request is aborted by user
/// </summary>
public class UserAbortException(string? message = null) : ApiException(errorDescription: message ?? "Request was aborted")
{
}

/// <summary>
/// Exception thrown when connection fails
/// </summary>
public class ConnectionException : ApiException
{
    public ConnectionException(string? message = null) : base(errorDescription: message ?? "Connection error") { }
    public ConnectionException(string? message, Exception innerException) : base(0, null, message ?? "Connection error", innerException) { }
}

/// <summary>
/// Exception thrown when request times out
/// </summary>
public class ConnectionTimeoutException(string? message = null) : ApiException(errorDescription: message ?? "Request timed out")
{
}

/// <summary>
/// Exception thrown for 400 Bad Request responses
/// </summary>
public class BadRequestException(string? errorCode = null, string? errorDescription = null, Dictionary<string, object>? headers = null) : ApiException(400, errorCode, errorDescription, headers)
{
}

/// <summary>
/// Exception thrown for 401 Unauthorized responses
/// </summary>
public class UnauthorizedException(string? errorCode = null, string? errorDescription = null, Dictionary<string, object>? headers = null) : ApiException(401, errorCode, errorDescription, headers)
{
}

/// <summary>
/// Exception thrown for 403 Forbidden responses
/// </summary>
public class ForbiddenException(string? errorCode = null, string? errorDescription = null, Dictionary<string, object>? headers = null) : ApiException(403, errorCode, errorDescription, headers)
{
}

/// <summary>
/// Exception thrown for 404 Not Found responses
/// </summary>
public class NotFoundException(string? errorCode = null, string? errorDescription = null, Dictionary<string, object>? headers = null) : ApiException(404, errorCode, errorDescription, headers)
{
}

/// <summary>
/// Exception thrown for 429 Too Many Requests responses
/// </summary>
public class TooManyRequestsException(string? errorCode = null, string? errorDescription = null, Dictionary<string, object>? headers = null) : ApiException(429, errorCode, errorDescription, headers)
{
}

/// <summary>
/// Exception thrown for 5xx Internal Server Error responses
/// </summary>
public class InternalServerErrorException(int statusCode, string? errorCode = null, string? errorDescription = null, Dictionary<string, object>? headers = null) : ApiException(statusCode, errorCode, errorDescription, headers)
{
}

/// <summary>
/// Exception thrown when signature validation fails
/// </summary>
public class InvalidSignatureException(string? message = null) : PayOSException(message ?? "Invalid signature")
{
}

/// <summary>
/// Exception thrown when webhook operations fail
/// </summary>
public class WebhookException(string? message = null) : PayOSException(message ?? "Webhook error")
{
}