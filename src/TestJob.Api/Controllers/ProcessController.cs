using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TestJob.Api.Models;
using TestJob.Api.Services;

namespace TestJob.Api.Controllers;

[ApiController]
[Route("api")]
[Produces("application/json")]
public sealed class ProcessController(PageProcessingService service) : ControllerBase
{
    // 10 MB of JSON ≈ 7.5 MB of HTML after Base64 overhead; large real pages are 1–3 MB.
    private const long MaxRequestBodySize = 10 * 1024 * 1024;

    [HttpPost("process")]
    [RequestSizeLimit(MaxRequestBodySize)]
    [Consumes("application/json")]
    [EndpointSummary("Parse a page, extract emails, decrypt the text and store the found elements")]
    [EndpointDescription("Maximum request body size is 10 MB (about 7.5 MB of HTML in page_b64). Larger requests are rejected with 413 REQUEST_TOO_LARGE.")]
    [ProducesResponseType<ProcessResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProcessResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProcessResponse>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProcessResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProcessResponse>> Process(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ProcessRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(ProcessResponse.Error(ErrorCodes.InvalidRequestBody, "Request body is empty."));
        }

        return Ok(await service.ProcessAsync(request, cancellationToken));
    }
}
