namespace InvoiceFlow.Application.Common.Interfaces;

/// <summary>
/// Abstracts Azure Blob Storage operations.
/// Blob paths follow the pattern: invoices/{year}/{month}/{guid}{ext}
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Uploads a file to Blob Storage and returns the blob path (not a full URL).
    /// </summary>
    Task<string> UploadAsync(
        Stream fileStream,
        string blobPath,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a short-lived read-only SAS URL for secure in-browser PDF preview.
    /// </summary>
    Task<Uri> GenerateSasUrlAsync(
        string blobPath,
        TimeSpan expiry,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string blobPath, CancellationToken cancellationToken = default);
}
