using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace PayOS.Models.V2.PaymentRequests.Invoices;

/// <summary>
/// Invoice information
/// </summary>
public class Invoice
{
    [JsonPropertyName("invoiceId")]
    public string InvoiceId { get; set; } = "";

    [JsonPropertyName("invoiceNumber")]
    public string? InvoiceNumber { get; set; }

    [JsonPropertyName("issuedTimestamp")]
    public long? IssuedTimestamp { get; set; }

    [JsonPropertyName("issuedDatetime")]
    public DateTime? IssuedDatetime { get; set; }

    [JsonPropertyName("transactionId")]
    public string? TransactionId { get; set; }

    [JsonPropertyName("reservationCode")]
    public string? ReservationCode { get; set; }

    [JsonPropertyName("codeOfTax")]
    public string? CodeOfTax { get; set; }
}

/// <summary>
/// Response containing invoices information
/// </summary>
public class InvoicesInfo
{
    [JsonPropertyName("invoices")]
    public List<Invoice> Invoices { get; set; } = [];
}

/// <summary>
/// File download response containing file data and metadata
/// </summary>
public class FileDownloadResponse : IDisposable
{
    /// <summary>
    /// The file content as a stream
    /// </summary>
    public Stream Content { get; set; } = Stream.Null;

    /// <summary>
    /// The content type of the file
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// The original filename
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// The file size in bytes
    /// </summary>
    public long? ContentLength { get; set; }

    /// <summary>
    /// Save the file content to the specified path
    /// </summary>
    /// <param name="filePath">The path where the file should be saved</param>
    /// <param name="requestOptions">Request options</param>
    /// <returns>The number of bytes written to the file</returns>
    public async Task<long> SaveToFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (Content == null)
            throw new InvalidOperationException("No content available to save");

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (Content.CanSeek)
        {
            Content.Position = 0;
        }

        using var fileStream = File.Create(filePath);
        await Content.CopyToAsync(fileStream);

        return fileStream.Length;
    }

    /// <summary>
    /// Save the file content using the original filename or a default name
    /// </summary>
    /// <param name="directory">The directory where the file should be saved (optional, defaults to current directory)</param>
    /// <param name="defaultFileName">Default filename</param>
    /// <param name="requestOptions">Request options</param>
    /// <returns>A tuple containing the full file path and the number of bytes written</returns>
    public async Task<(string FilePath, long BytesWritten)> SaveToDirectoryAsync(string? directory = null, string? defaultFileName = null, CancellationToken cancellationToken = default)
    {
        var fileName = defaultFileName ?? FileName ?? "download";
        var fullPath = string.IsNullOrEmpty(directory) ? fileName : Path.Combine(directory, fileName);

        var bytesWritten = await SaveToFileAsync(fullPath, cancellationToken);
        return (fullPath, bytesWritten);
    }

    /// <summary>
    /// Dispose the content stream
    /// </summary>
    public void Dispose()
    {
        Content?.Dispose();
        GC.SuppressFinalize(this);
    }
}