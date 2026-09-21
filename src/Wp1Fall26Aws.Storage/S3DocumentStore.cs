using System.Globalization;
using Microsoft.Extensions.Options;

namespace Wp1Fall26Aws.Storage;

public sealed class S3DocumentStore : IDocumentStore
{
    private readonly IS3ObjectClient _client;
    private readonly DocumentValidator _validator;
    private readonly S3StorageOptions _options;

    public S3DocumentStore(IS3ObjectClient client, DocumentValidator validator, IOptions<S3StorageOptions> options)
    {
        _client = client;
        _validator = validator;
        _options = options.Value;
    }

    public async Task<StoreResult> StoreAsync(DocumentUpload upload, CancellationToken ct = default)
    {
        var errors = _validator.Validate(upload);
        if (errors.Count > 0)
        {
            return new StoreResult(false, null, errors, null);
        }

        // Checked after validation so a bad upload is still a 400. A missing
        // bucket is a server misconfiguration, surfaced at the first real upload.
        if (string.IsNullOrWhiteSpace(_options.BucketName))
        {
            throw new InvalidOperationException("S3Storage:BucketName is not configured.");
        }

        var now = DateTime.UtcNow;
        var key = BuildKey(now, upload.FileName);
        var metadata = new Dictionary<string, string>
        {
            ["original-filename"] = upload.FileName,
            ["content-type"] = upload.ContentType,
            ["uploaded-utc"] = now.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
            ["content-length"] = upload.ContentLength.ToString(CultureInfo.InvariantCulture),
        };

        await _client.PutObjectAsync(_options.BucketName, key, upload.Content, upload.ContentType, metadata, ct);

        return new StoreResult(true, key, [], metadata);
    }

    // The original filename never becomes part of the key, only its extension.
    private string BuildKey(DateTime utcNow, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var prefix = _options.KeyPrefix.Trim('/');
        var name = $"{utcNow:yyyy}/{utcNow:MM}/{utcNow:dd}/{Guid.NewGuid():N}{extension}";
        return prefix.Length == 0 ? name : $"{prefix}/{name}";
    }
}
