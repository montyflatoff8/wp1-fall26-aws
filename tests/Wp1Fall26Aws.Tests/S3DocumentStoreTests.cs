using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Wp1Fall26Aws.Storage;

namespace Wp1Fall26Aws.Tests;

public class S3DocumentStoreTests
{
    private const string Bucket = "test-bucket";

    private static (S3DocumentStore Store, FakeS3ObjectClient Fake) CreateStore(string bucket = Bucket)
    {
        var options = Options.Create(new S3StorageOptions { BucketName = bucket });
        var fake = new FakeS3ObjectClient();
        return (new S3DocumentStore(fake, new DocumentValidator(options), options), fake);
    }

    private static DocumentUpload Upload(string fileName = "notes.txt", string contentType = "text/plain", string body = "hello world") =>
        new(fileName, contentType, Encoding.UTF8.GetByteCount(body), new MemoryStream(Encoding.UTF8.GetBytes(body)));

    [Fact]
    public async Task Key_has_the_documented_format()
    {
        var (store, fake) = CreateStore();

        var result = await store.StoreAsync(Upload());

        Assert.True(result.Success);
        Assert.Matches(@"^documents/uploads/\d{4}/\d{2}/\d{2}/[0-9a-f]{32}\.txt$", result.ObjectKey);
        Assert.Equal(result.ObjectKey, Assert.Single(fake.Calls).Key);
    }

    [Fact]
    public async Task Key_uses_the_lowercased_extension_and_never_the_original_filename()
    {
        var (store, _) = CreateStore();

        var result = await store.StoreAsync(Upload("Quarterly REPORT.PDF", "application/pdf"));

        Assert.EndsWith(".pdf", result.ObjectKey);
        Assert.DoesNotContain("Quarterly", result.ObjectKey);
        Assert.DoesNotContain("REPORT", result.ObjectKey);
    }

    [Fact]
    public async Task Two_uploads_of_the_same_filename_get_different_keys()
    {
        var (store, _) = CreateStore();

        var first = await store.StoreAsync(Upload());
        var second = await store.StoreAsync(Upload());

        Assert.NotEqual(first.ObjectKey, second.ObjectKey);
    }

    [Fact]
    public async Task Metadata_has_exactly_the_four_documented_entries()
    {
        var (store, fake) = CreateStore();
        var before = DateTime.UtcNow.AddSeconds(-1);

        var result = await store.StoreAsync(Upload("notes.txt", "text/plain", "hello world"));

        var metadata = Assert.Single(fake.Calls).Metadata;
        Assert.Equal(4, metadata.Count);
        Assert.Equal("notes.txt", metadata["original-filename"]);
        Assert.Equal("text/plain", metadata["content-type"]);
        Assert.Equal("11", metadata["content-length"]);

        Assert.Matches(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$", metadata["uploaded-utc"]);
        var uploaded = DateTime.ParseExact(
            metadata["uploaded-utc"], "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        Assert.InRange(uploaded, before, DateTime.UtcNow.AddSeconds(1));

        Assert.Equal(metadata, result.Metadata);
    }

    [Fact]
    public async Task Rejected_upload_never_calls_S3()
    {
        var (store, fake) = CreateStore();

        var result = await store.StoreAsync(Upload("setup.exe", "application/x-msdownload"));

        Assert.False(result.Success);
        Assert.Null(result.ObjectKey);
        Assert.Equal(2, result.Errors.Count);
        Assert.Empty(fake.Calls);
    }

    [Fact]
    public async Task Configured_bucket_name_is_passed_to_S3()
    {
        var (store, fake) = CreateStore("my-configured-bucket");

        await store.StoreAsync(Upload());

        var call = Assert.Single(fake.Calls);
        Assert.Equal("my-configured-bucket", call.Bucket);
        Assert.Equal("text/plain", call.ContentType);
    }

    [Fact]
    public async Task Missing_bucket_name_fails_the_upload_without_calling_S3()
    {
        var (store, fake) = CreateStore(bucket: "");

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.StoreAsync(Upload()));

        Assert.Empty(fake.Calls);
    }
}
