namespace Wp1Fall26Aws.Storage;

/// <summary>
/// What the API hands down. No HttpContext, no IFormFile - those are web types
/// and they do not belong below the boundary.
/// </summary>
public sealed record DocumentUpload(
    string FileName,
    string ContentType,
    long ContentLength,
    Stream Content);
