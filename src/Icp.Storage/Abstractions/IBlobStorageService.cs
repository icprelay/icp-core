namespace Icp.Storage.Abstractions
{
    public interface IBlobStorageService
    {
        Task<Uri> UploadAsync(
            string container,
            string blobName,
            Stream content,
            string contentType,
            CancellationToken ct = default
        );

        Task<Stream> DownloadAsync(
            string container,
            string blobName,
            CancellationToken ct = default
        );

        /// <summary>
        /// Download a blob by its full path, which includes both container and blob name. This is useful when the caller only has the full path of the blob (e.g. stored in a database) and doesn't want to parse out the container and blob name.
        /// </summary>
        /// <param name="fullBlobPath">e.g. /icp-sink-blob/2026-02-19-4f9f0192-ed74-468c-8ed9-8dd6a4a57916.json</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Stream> DownloadAsync(
            string fullBlobPath,
            CancellationToken ct = default
        );
    }
}
