using Wp1Fall26Aws.Storage;

namespace Wp1Fall26Aws.Tests;

public sealed record PutObjectCall(
    string Bucket,
    string Key,
    string ContentType,
    IReadOnlyDictionary<string, string> Metadata);

/// <summary>Records every PutObjectAsync call; never touches the network.</summary>
public sealed class FakeS3ObjectClient : IS3ObjectClient
{
    public List<PutObjectCall> Calls { get; } = [];

    public Task PutObjectAsync(
        string bucket,
        string key,
        Stream content,
        string contentType,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken ct = default)
    {
        Calls.Add(new PutObjectCall(bucket, key, contentType, metadata));
        return Task.CompletedTask;
    }
}
