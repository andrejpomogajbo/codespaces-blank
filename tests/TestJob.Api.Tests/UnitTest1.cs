using System.Text;
using TestJob.Api.Models;
using TestJob.Api.Validators;

namespace TestJob.Api.Tests;

public class ProcessRequestRequestValidatorTests
{
    [Fact]
    public void Validate_WhenRequiredFieldsAreFilled_ReturnsSuccess()
    {
        var validator = new ProcessRequestRequestValidator();
        var request = new ProcessRequestRequest
        {
            Selector = "a[href]",
            Attribute = "href",
            UrlB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("https://example.com/page1")),
            EncryptedTextBytesB64 = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 }),
            KeyBytesB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("TestKey1234567890")),
            PageB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("<html><body><a href=\"https://example.com\">Link</a></body></html>"))
        };

        var result = validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WhenSelectorIsMissingOrEmpty_ReturnsFailure(string? selector)
    {
        var validator = new ProcessRequestRequestValidator();
        var request = new ProcessRequestRequest
        {
            Selector = selector,
            Attribute = "href",
            UrlB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("https://example.com")),
            EncryptedTextBytesB64 = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 }),
            KeyBytesB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("TestKey1234567890")),
            PageB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("<html></html>"))
        };

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ProcessRequestRequest.Selector));
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("\r\n")]
    public void Validate_WhenSelectorOrAttributeContainsOnlyWhitespace_ReturnsFailure(string whitespace)
    {
        var validator = new ProcessRequestRequestValidator();
        var request = new ProcessRequestRequest
        {
            Selector = whitespace,
            Attribute = whitespace,
            UrlB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("https://example.com")),
            EncryptedTextBytesB64 = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 }),
            KeyBytesB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("TestKey1234567890")),
            PageB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("<html></html>"))
        };

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ProcessRequestRequest.Selector));
    }
}

