using Jetar.Application.Contracts;
using Jetar.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jetar.API.Controllers;

[ApiController]
[Route("api/boosts")]
public class BoostsController : ControllerBase
{
    private readonly IBoostService _boosts;
    private readonly ICurrentUser _user;

    public BoostsController(IBoostService boosts, ICurrentUser user)
    {
        _boosts = boosts;
        _user = user;
    }

    /// <summary>Karta rekvizitlari va tariflar — ko'tarish oynasida ko'rsatiladi.</summary>
    [HttpGet("config")]
    public ActionResult<BoostConfigDto> Config() => Ok(_boosts.GetConfig());

    /// <summary>Sotuvchi to'lov qildim deb so'rov yuboradi.</summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<BoostRequestDto>> Create(CreateBoostRequest request, CancellationToken ct)
        => Ok(await _boosts.RequestAsync(_user.RequireId(), request, ct));

    /// <summary>Mening ko'tarish so'rovlarim.</summary>
    [HttpGet("mine")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<BoostRequestDto>>> Mine(CancellationToken ct)
        => Ok(await _boosts.GetMineAsync(_user.RequireId(), ct));
}
