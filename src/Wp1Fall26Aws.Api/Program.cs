using Amazon.S3;
using Wp1Fall26Aws.Storage;

var builder = WebApplication.CreateBuilder(args);

// Deliberately no explicit URL binding on the web host: the Beanstalk Procfile
// passes --urls, and code that sets URLs here would override it.

builder.Services.Configure<S3StorageOptions>(builder.Configuration.GetSection("S3Storage"));

// Credentials and region come from the SDK's default chain only (SSO locally,
// instance profile on Beanstalk). Nothing is configured explicitly here.
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddS3DocumentStorage();

var app = builder.Build();

app.MapGet("/", () => "Wp1Fall26Aws document upload API");

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapPost("/documents", async (
    HttpRequest request,
    IServiceProvider services,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    var noFile = Results.BadRequest(new { errors = new[] { "No file was provided." } });

    if (!request.HasFormContentType)
    {
        return noFile;
    }

    IFormCollection form;
    try
    {
        form = await request.ReadFormAsync(ct);
    }
    catch (InvalidDataException)
    {
        return noFile;
    }

    var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
    if (file is null)
    {
        return noFile;
    }

    try
    {
        // Resolved inside the try on purpose: building the S3 client resolves AWS
        // credentials, and a failure there must get the same generic 500 as any
        // other storage failure instead of escaping as an unhandled exception.
        var store = services.GetRequiredService<IDocumentStore>();

        await using var content = file.OpenReadStream();
        var upload = new DocumentUpload(file.FileName, file.ContentType, file.Length, content);
        var result = await store.StoreAsync(upload, ct);

        return result.Success
            ? Results.Json(new { objectKey = result.ObjectKey, metadata = result.Metadata }, statusCode: StatusCodes.Status201Created)
            : Results.BadRequest(new { errors = result.Errors });
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
        // Storage errors can carry bucket names, ARNs, or request IDs. Log them
        // here, and give the caller nothing to work with.
        logger.LogError(ex, "Storing an uploaded document failed.");
        return Results.Json(new { errors = new[] { "The document could not be stored." } }, statusCode: StatusCodes.Status500InternalServerError);
    }
});

app.Run();
