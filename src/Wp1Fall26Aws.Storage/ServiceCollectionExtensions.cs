using Microsoft.Extensions.DependencyInjection;

namespace Wp1Fall26Aws.Storage;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the S3-backed document store. The caller supplies IAmazonS3
    /// (via AddAWSService) and binds S3StorageOptions.
    /// </summary>
    public static IServiceCollection AddS3DocumentStorage(this IServiceCollection services)
    {
        services.AddSingleton<DocumentValidator>();
        services.AddSingleton<IS3ObjectClient, AwsS3ObjectClient>();
        services.AddSingleton<IDocumentStore, S3DocumentStore>();
        return services;
    }
}
