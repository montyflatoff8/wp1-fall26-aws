namespace Wp1Fall26Aws.Storage;

/// <summary>
/// What comes back. A result object, not an exception, because rejection is an
/// expected outcome here rather than an error.
/// </summary>
public sealed record StoreResult(
    bool Success,
    string? ObjectKey,
    IReadOnlyList<string> Errors,
    IReadOnlyDictionary<string, string>? Metadata);
