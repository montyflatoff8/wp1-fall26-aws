using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Wp1Fall26Aws.Storage;

namespace Wp1Fall26Aws.Tests;

public class DocumentsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DocumentsEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task PostDocuments_ValidTextFile_Returns201AndUploadsOnceThroughFake()
    {
        // Arrange
        var fake = new FakeS3ObjectClient();
        var client = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IS3ObjectClient>(fake);
                services.PostConfigure<S3StorageOptions>(o => o.BucketName = "integration-test-bucket");
            })).CreateClient();

        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes("hello from the integration test"));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        form.Add(file, "file", "notes.txt");

        // Act
        var response = await client.PostAsync("/documents", form);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var call = Assert.Single(fake.Calls);
        Assert.Equal("integration-test-bucket", call.Bucket);
        var body = await response.Content.ReadFromJsonAsync<UploadResponse>();
        Assert.Equal(call.Key, body!.ObjectKey);
    }

    private sealed record UploadResponse(string ObjectKey);
}