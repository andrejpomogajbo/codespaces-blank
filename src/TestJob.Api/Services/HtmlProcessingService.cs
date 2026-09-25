using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Dapper;
using FluentValidation;
using Npgsql;
using TestJob.Api.Models;
using TestJob.Api.Validators;

namespace TestJob.Api.Services;

public sealed class Base64DecodeException : Exception
{
    public Base64DecodeException(string fieldName, Exception? innerException = null)
        : base($"Failed to decode base64 for '{fieldName}'.", innerException)
    {
        FieldName = fieldName;
        ErrorCode = fieldName switch
        {
            "url" => "URL_BASE64_DECODE_ERROR",
            "page" => "PAGE_BASE64_DECODE_ERROR",
            _ => "BASE64_DECODE_ERROR"
        };
    }

    public string FieldName { get; }

    public string ErrorCode { get; }
}

public sealed class HtmlProcessingService
{
    private const string ConnectionStringKey = "DefaultConnection";
    private static readonly Regex EmailRegex = new(
        @"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private readonly string _connectionString;
    private readonly ProcessRequestRequestValidator _validator;

    public HtmlProcessingService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString(ConnectionStringKey)
            ?? "Host=localhost;Port=5432;Database=appdb;Username=appuser;Password=appsecret";
        _validator = new ProcessRequestRequestValidator();
    }

    public async Task InitializeDatabaseAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"CREATE TABLE IF NOT EXISTS elements (
            id BIGSERIAL PRIMARY KEY,
            attribute_value TEXT NOT NULL,
            full_html TEXT NOT NULL
        );";

        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    public async Task<ProcessApiResponse> ProcessAsync(ProcessRequestRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validationResult = _validator.Validate(request);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(string.Join("; ", validationResult.Errors.Select(x => x.ErrorMessage)));
        }

        var url = DecodeBase64String(request.UrlB64!, "url");
        var pageHtml = DecodeBase64String(request.PageB64!, "page");

        var parser = new HtmlParser();
        var document = await parser.ParseDocumentAsync(pageHtml, cancellationToken);
        var selectedElements = document.QuerySelectorAll(request.Selector!);

        var attributeValues = new List<string>();
        var fullHtmlValues = new List<string>();

        foreach (var element in selectedElements)
        {
            var attributeValue = element.GetAttribute(request.Attribute!) ?? string.Empty;
            attributeValues.Add(attributeValue);
            fullHtmlValues.Add(element.OuterHtml ?? string.Empty);
        }

        await SaveElementsAsync(attributeValues, fullHtmlValues, cancellationToken);

        var emails = EmailRegex.Matches(pageHtml)
            .Select(match => match.Value)
            .ToList();

        var decryptedPlainText = DecryptText(request.EncryptedTextBytesB64!, request.KeyBytesB64!);

        return new ProcessApiResponse
        {
            IsError = 0,
            ErrorCode = string.Empty,
            ErrorMessage = string.Empty,
            ElementsCount = selectedElements.Length,
            EmailsCount = emails.Count,
            Url = url,
            DecryptedPlainText = decryptedPlainText,
            ElementsAttrList = attributeValues,
            EmailsList = emails
        };
    }

    private async Task SaveElementsAsync(IReadOnlyCollection<string> attributeValues, IReadOnlyCollection<string> fullHtmlValues, CancellationToken cancellationToken)
    {
        if (attributeValues.Count == 0 || fullHtmlValues.Count == 0)
        {
            return;
        }

        var zippedItems = attributeValues.Zip(fullHtmlValues, (attributeValue, fullHtml) => new
        {
            AttributeValue = attributeValue,
            FullHtml = fullHtml
        }).ToList();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string insertSql = "INSERT INTO elements (attribute_value, full_html) VALUES (@AttributeValue, @FullHtml);";

        foreach (var item in zippedItems)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(insertSql, new { item.AttributeValue, item.FullHtml }, cancellationToken: cancellationToken));
        }
    }

    private static string DecodeBase64String(string value, string target)
    {
        try
        {
            var bytes = Convert.FromBase64String(value);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (FormatException ex)
        {
            throw new Base64DecodeException(target, ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to decode base64 for {target}.", ex);
        }
    }

    private static string DecryptText(string encryptedTextBytesB64, string keyBytesB64)
    {
        try
        {
            var encryptedBytes = Convert.FromBase64String(encryptedTextBytesB64);
            var keyBytes = Convert.FromBase64String(keyBytesB64);

            using var aes = Aes.Create();
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;

            using var decryptor = aes.CreateDecryptor(keyBytes, null);
            using var memoryStream = new MemoryStream();
            using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Write))
            {
                cryptoStream.Write(encryptedBytes, 0, encryptedBytes.Length);
                cryptoStream.FlushFinalBlock();
            }

            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to decrypt data using AES-256 ECB with PaddingMode.None.", ex);
        }
    }
}
