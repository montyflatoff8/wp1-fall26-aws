namespace Wp1Fall26Aws.Storage;

/// <summary>
/// The seam between S3DocumentStore and the AWS SDK. Narrow on purpose: tests
/// substitute a recording fake, so nothing below the API needs a real S3 client.
/// </summary>
public interface IS3ObjectClient
{
    Task PutObjectAsync(
        string bucket,
        string key,
        Stream content,
        string contentType,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken ct = default);
}
