using System.Text.RegularExpressions;
using IRISA.Backend.Dtos;
using IRISA.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IRISA.Backend.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/people")]
public sealed partial class PeopleController(
    IPersonLookupService personLookupService) : ControllerBase
{
    [HttpGet("{nationalCode}")]
    [ProducesResponseType<PersonLookupResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<PersonLookupResponse>> Find(
        string nationalCode,
        CancellationToken cancellationToken)
    {
        if (!NationalCodePattern().IsMatch(nationalCode))
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "کد ملی نامعتبر",
                Detail = "کد ملی باید دقیقاً ۱۰ رقم باشد."
            });

        var result = await personLookupService.FindAsync(nationalCode, cancellationToken);
        if (result.Person is not null)
            return Ok(result.Person);

        if (result.SourceUnavailable)
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new ProblemDetails
                {
                    Status = StatusCodes.Status503ServiceUnavailable,
                    Title = "سرویس اطلاعات در دسترس نیست",
                    Detail = "نسخهٔ ذخیره‌شده‌ای برای این کد ملی وجود ندارد."
                });

        return NotFound(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "اطلاعاتی پیدا نشد",
            Detail = "برای این کد ملی در منبع اصلی اطلاعاتی ثبت نشده است."
        });
    }

    [HttpPost("{nationalCode}/refresh")]
    [ProducesResponseType<PersonLookupResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<PersonLookupResponse>> Refresh(
        string nationalCode,
        CancellationToken cancellationToken)
    {
        if (!NationalCodePattern().IsMatch(nationalCode))
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "کد ملی نامعتبر",
                Detail = "کد ملی باید دقیقاً ۱۰ رقم باشد."
            });

        var result = await personLookupService.RefreshFromSourceAsync(nationalCode, cancellationToken);
        if (result.Person is not null)
            return Ok(result.Person);

        if (result.SourceUnavailable)
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new ProblemDetails
                {
                    Status = StatusCodes.Status503ServiceUnavailable,
                    Title = "سرویس اطلاعات در دسترس نیست",
                    Detail = "در حال حاضر منبع اصلی در دسترس نیست و هیچ نسخهٔ ذخیره‌شده‌ای برای این پرونده یافت نشد."
                });

        return NotFound(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "اطلاعاتی پیدا نشد",
            Detail = "برای این کد ملی در منبع اصلی اطلاعاتی ثبت نشده است."
        });
    }

    [GeneratedRegex(@"^\d{10}$", RegexOptions.CultureInvariant)]
    private static partial Regex NationalCodePattern();
}
