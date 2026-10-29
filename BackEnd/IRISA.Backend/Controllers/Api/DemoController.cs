using IRISA.Backend.Data;
using IRISA.Backend.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IRISA.Backend.Controllers.Api;

[ApiController]
[Route("api/demo")]
public sealed class DemoController(
    IConfiguration configuration,
    SourceDataContext sourceContext) : ControllerBase
{
    [HttpGet("records")]
    [Authorize]
    [ProducesResponseType<IReadOnlyList<DemoPersonResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DemoPersonResponse>>> GetRecords(
        CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("DemoData:SeedSourceDatabase"))
            return NotFound();

        var records = await sourceContext.People
            .AsNoTracking()
            .OrderBy(person => person.NationalCode)
            .Select(person => new DemoPersonResponse(person.NationalCode, person.FullName))
            .ToArrayAsync(cancellationToken);

        return Ok(records);
    }
}
