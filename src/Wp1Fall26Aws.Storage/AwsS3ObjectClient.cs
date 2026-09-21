using Amazon.S3;
using Amazon.S3.Model;

namespace Wp1Fall26Aws.Storage;

/// <summary>
/// Thin real implementation of IS3ObjectClient. It builds no credentials of its
/// own; the IAmazonS3 it receives comes from the SDK's default chain.
/// </summary>
public sealed class AwsS3ObjectClient : IS3ObjectClient
{
    private readonly IAmazonS3 _s3;

    public AwsS3ObjectClient(IAmazonS3 s3) => _s3 = s3;

    public async Task PutObjectAsync(
        string bucket,
        string key,
        Stream content,
        string contentType,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
        };

        foreach (var (name, value) in metadata)
        {
            request.Metadata[name] = value;
        }

        await _s3.PutObjectAsync(request, ct);
    }
}
