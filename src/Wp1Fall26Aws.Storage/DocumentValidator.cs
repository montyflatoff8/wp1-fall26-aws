using Microsoft.Extensions.Options;

namespace Wp1Fall26Aws.Storage;

/// <summary>
/// Enforces the upload policy. Returns every violated rule, not just the first.
/// </summary>
public sealed class DocumentValidator
{
    private readonly S3StorageOptions _options;
    private readonly HashSet<string> _extensions;
    private readonly HashSet<string> _contentTypes;

    public DocumentValidator(IOptions<S3StorageOptions> options)
    {
        _options = options.Value;
        _extensions = new HashSet<string>(_options.AllowedExtensions, StringComparer.OrdinalIgnoreCase);
        _contentTypes = new HashSet<string>(_options.AllowedContentTypes, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<string> Validate(DocumentUpload upload)
    {
        var errors = new List<string>();

        if (upload.ContentLength <= 0)
        {
            errors.Add("File is empty.");
        }
        else if (upload.ContentLength > _options.MaxFileSizeBytes)
        {
            errors.Add($"File exceeds the maximum size of {_options.MaxFileSizeBytes} bytes.");
        }

        var extension = Path.GetExtension(upload.FileName);
        if (!_extensions.Contains(extension))
        {
            errors.Add($"Extension '{extension}' is not allowed.");
        }

        if (!_contentTypes.Contains(MediaTypeOnly(upload.ContentType)))
        {
            errors.Add($"Content type '{upload.ContentType}' is not allowed.");
        }

        return errors;
    }

    // "text/plain; charset=utf-8" should match an allowed "text/plain".
    private static string MediaTypeOnly(string contentType)
    {
        var separator = contentType.IndexOf(';');
        return (separator < 0 ? contentType : contentType[..separator]).Trim();
    }
}
