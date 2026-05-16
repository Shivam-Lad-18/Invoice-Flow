using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using InvoiceFlow.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace InvoiceFlow.Infrastructure.Storage;

/// <summary>
/// Uploads and manages invoice PDFs in Azure Blob Storage.
/// For local dev, point "Azure:BlobStorage:ConnectionString" to Azurite:
///   UseDevelopmentStorage=true
/// </summary>
public sealed class BlobStorageService(IConfiguration configuration) : IBlobStorageService
{
    private readonly string _connectionString =
        configuration["Azure:BlobStorage:ConnectionString"]
        ?? throw new InvalidOperationException("Azure:BlobStorage:ConnectionString is not configured.");

    private readonly string _container =
        configuration["Azure:BlobStorage:ContainerName"] ?? "invoices";

    private BlobContainerClient ContainerClient =>
        new BlobServiceClient(_connectionString).GetBlobContainerClient(_container);

    public async Task<string> UploadAsync(
        Stream fileStream,
        string blobPath,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var container = ContainerClient;
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blob = container.GetBlobClient(blobPath);
        await blob.UploadAsync(fileStream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        }, cancellationToken);

        return blobPath;
    }

    public Task<Uri> GenerateSasUrlAsync(
        string blobPath,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        var blob = ContainerClient.GetBlobClient(blobPath);
        var sasUri = blob.GenerateSasUri(
            BlobSasPermissions.Read,
            DateTimeOffset.UtcNow.Add(expiry));

        return Task.FromResult(sasUri);
    }

    public async Task DeleteAsync(string blobPath, CancellationToken cancellationToken = default)
    {
        var blob = ContainerClient.GetBlobClient(blobPath);
        await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }
}
