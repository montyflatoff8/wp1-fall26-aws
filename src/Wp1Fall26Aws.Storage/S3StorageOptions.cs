namespace Wp1Fall26Aws.Storage;

/// <summary>
/// Bound from the "S3Storage" configuration section. The same keys arrive from
/// user secrets (S3Storage:BucketName), Beanstalk environment properties
/// (S3Storage__BucketName), or code in tests.
/// </summary>
public sealed class S3StorageOptions
{
    public const string SectionName = "S3Storage";

    public string BucketName { get; set; } = string.Empty;

    public string KeyPrefix { get; set; } = "documents/uploads";

    // Documents the intended region. Client construction never reads it: the
    // SDK's default chain resolves region and credentials.
    public string Region { get; set; } = "us-east-1";

    public long MaxFileSizeBytes { get; set; } = 1_048_576;

    public string[] AllowedExtensions { get; set; } = [".txt", ".pdf", ".png", ".jpg", ".jpeg"];

    public string[] AllowedContentTypes { get; set; } = ["text/plain", "application/pdf", "image/png", "image/jpeg"];
}
