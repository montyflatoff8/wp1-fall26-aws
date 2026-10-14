using Microsoft.Extensions.Options;
using Wp1Fall26Aws.Storage;

namespace Wp1Fall26Aws.Tests;

public class DocumentValidatorTests
{
    private static readonly DocumentValidator Validator = new(Options.Create(new S3StorageOptions()));

    private static DocumentUpload Upload(string fileName, string contentType, long length) =>
        new(fileName, contentType, length, Stream.Null);

    [Fact]
    public void Accepts_valid_small_txt()
    {
        var errors = Validator.Validate(Upload("notes.txt", "text/plain", 100));

        Assert.Empty(errors);
    }

    [Fact]
    public void Rejects_empty_file()
    {
        var errors = Validator.Validate(Upload("notes.txt", "text/plain", 0));

        Assert.Equal(["File is empty."], errors);
    }

    [Fact]
    public void Accepts_file_of_exactly_the_maximum_size()
    {
        var errors = Validator.Validate(Upload("notes.txt", "text/plain", 1_048_576));

        Assert.Empty(errors);
    }

    [Fact]
    public void Rejects_file_one_byte_over_the_maximum_size()
    {
        var errors = Validator.Validate(Upload("notes.txt", "text/plain", 1_048_577));

        Assert.Equal(["File exceeds the maximum size of 1048576 bytes."], errors);
    }

    [Fact]
    public void Rejects_disallowed_extension()
    {
        var errors = Validator.Validate(Upload("setup.exe", "text/plain", 100));

        Assert.Equal(["Extension '.exe' is not allowed."], errors);
    }

    [Fact]
    public void Rejects_disallowed_content_type()
    {
        var errors = Validator.Validate(Upload("notes.txt", "application/x-msdownload", 100));

        Assert.Equal(["Content type 'application/x-msdownload' is not allowed."], errors);
    }

    [Fact]
    public void Accepts_uppercase_extension()
    {
        var errors = Validator.Validate(Upload("REPORT.PDF", "application/pdf", 100));

        Assert.Empty(errors);
    }

    [Fact]
    public void Accepts_content_type_with_parameters()
    {
        var errors = Validator.Validate(Upload("notes.txt", "text/plain; charset=utf-8", 100));

        Assert.Empty(errors);
    }

    [Fact]
    public void Returns_every_error_when_several_rules_fail()
    {
        var errors = Validator.Validate(Upload("malware.exe", "application/x-msdownload", 0));

        Assert.Equal(
            [
                "File is empty.",
                "Extension '.exe' is not allowed.",
                "Content type 'application/x-msdownload' is not allowed.",
            ],
            errors);
    }

    [Fact]
    public void Validate_FileNameWithoutExtension_ReturnsExtensionNotAllowedError()
    {
        // Arrange
        var upload = Upload("README", "text/plain", 100);

        // Act
        var errors = Validator.Validate(upload);

        // Assert
        Assert.Equal(["This is deliberately wrong."], errors);
    }
}
