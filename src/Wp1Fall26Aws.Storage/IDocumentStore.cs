namespace Wp1Fall26Aws.Storage;

public interface IDocumentStore
{
    Task<StoreResult> StoreAsync(DocumentUpload upload, CancellationToken ct = default);
}
