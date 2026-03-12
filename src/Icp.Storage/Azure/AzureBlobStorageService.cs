using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Icp.Storage.Abstractions;
using System.ComponentModel;

namespace Icp.Storage.Azure;

public sealed class AzureBlobStorageService : IBlobStorageService
{
    private readonly StorageOptions options;
    private readonly BlobServiceClient serviceClient;

    public AzureBlobStorageService(StorageOptions options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));

        if (!string.IsNullOrWhiteSpace(options.Domain))
        {
            serviceClient = new BlobServiceClient(new Uri(options.Domain), new DefaultAzureCredential());
        }
        else
        {
            throw new InvalidOperationException("StorageOptions must provide Domain.");
        }
    }

    public async Task<Uri> UploadAsync(
        string container,
        string blobName,
        Stream content,
        string contentType,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(container))
            throw new ArgumentException("container is required", nameof(container));
        if (string.IsNullOrWhiteSpace(blobName))
            throw new ArgumentException("blobName is required", nameof(blobName));
        if (content is null)
            throw new ArgumentNullException(nameof(content));
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("contentType is required", nameof(contentType));

        var containerClient = serviceClient.GetBlobContainerClient(container);

        if (options.CreateContainerIfMissing)
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);

        var blobClient = containerClient.GetBlobClient(blobName);

        content.Position = 0;
        await blobClient.UploadAsync(
            content,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            },
            ct);

        return blobClient.Uri;
    }

    public async Task<Stream> DownloadAsync(
        string container,
        string blobName,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(container))
            throw new ArgumentException("container is required", nameof(container));
        if (string.IsNullOrWhiteSpace(blobName))
            throw new ArgumentException("blobName is required", nameof(blobName));

        var containerClient = serviceClient.GetBlobContainerClient(container);
        var blobClient = containerClient.GetBlobClient(blobName);

        var resp = await blobClient.DownloadStreamingAsync(cancellationToken: ct);
        var ms = new MemoryStream();
        await resp.Value.Content.CopyToAsync(ms, ct);
        ms.Position = 0;
        return ms;
    }

    public Task<Stream> DownloadAsync(string fullBlobPath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fullBlobPath))
            throw new ArgumentException("fullBlobPath is required", nameof(fullBlobPath));

        var parts = fullBlobPath.Split('/', 3);
        if (parts.Length < 3)
            throw new ArgumentException("fullBlobPath must include both container and blob name", nameof(fullBlobPath));

        var container = parts[1];
        var blobName = parts[2];

        return DownloadAsync(container, blobName, ct);
    }
}
