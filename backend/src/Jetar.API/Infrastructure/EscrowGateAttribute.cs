using Jetar.Domain.Common;
using Jetar.Application.Options;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Jetar.API.Infrastructure;

/// <summary>
/// Escrow/to'lov/bitim endpoint'larini <see cref="PlatformOptions.EscrowEnabled"/> bayrog'iga bog'laydi.
/// Yangi modelda platforma pulga aralashmaydi — bayroq false bo'lsa 410 (Gone) qaytariladi.
/// Kod o'chirilmagan: bayroqni true qilib eski oqimni qayta yoqish mumkin.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class EscrowGateAttribute : Attribute, IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var options = context.HttpContext.RequestServices.GetRequiredService<IOptions<PlatformOptions>>().Value;

        if (!options.EscrowEnabled)
            throw new AppException(
                "Escrow/to'lov xizmati o'chirilgan. Sotuvchi bilan to'g'ridan-to'g'ri bog'laning.",
                410, "escrow_disabled");
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
