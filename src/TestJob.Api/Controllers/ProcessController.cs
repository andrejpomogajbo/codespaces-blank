using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TestJob.Api.Models;
using TestJob.Api.Services;

namespace TestJob.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class ProcessController : ControllerBase
{
    private readonly HtmlProcessingService _service;

    public ProcessController(HtmlProcessingService service)
    {
        _service = service;
    }

    [HttpPost("process")]
    [ProducesResponseType(typeof(ProcessApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProcessApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProcessApiResponse>> ProcessAsync([FromBody] ProcessRequestRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.ProcessAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(CreateErrorResponse("VALIDATION_ERROR", ex.Message));
        }
        catch (Base64DecodeException ex)
        {
            return BadRequest(CreateErrorResponse(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(CreateErrorResponse("UNEXPECTED_ERROR", ex.Message));
        }
    }

    private static ProcessApiResponse CreateErrorResponse(string errorCode, string errorMessage)
    {
        return new ProcessApiResponse
        {
            IsError = 1,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            ElementsCount = 0,
            EmailsCount = 0,
            Url = string.Empty,
            DecryptedPlainText = string.Empty,
            ElementsAttrList = new List<string>(),
            EmailsList = new List<string>()
        };
    }
}
