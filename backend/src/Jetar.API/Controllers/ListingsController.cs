using Jetar.Core.Common;
using Jetar.Core.Contracts;
using Jetar.Core.Interfaces;
using Jetar.Core.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Jetar.API.Controllers;

[ApiController]
[Route("api/listings")]
public class ListingsController : ControllerBase
{
    private readonly IListingService _listings;
    private readonly ICurrentUser _user;
    private readonly IFileStorage _storage;
    private readonly PlatformOptions _platform;

    public ListingsController(
        IListingService listings,
        ICurrentUser user,
        IFileStorage storage,
        IOptions<PlatformOptions> platform)
    {
        _listings = listings;
        _user = user;
        _storage = storage;
        _platform = platform.Value;
    }

    /// <summary>E'lonlar ro'yxati — filtr, qidiruv va saralash bilan.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ListingCardDto>>> Search([FromQuery] ListingQuery query, CancellationToken ct)
        => Ok(await _listings.SearchAsync(query, ct));

    /// <summary>O'yinlar va ular bo'yicha e'lonlar soni.</summary>
    [HttpGet("games")]
    public async Task<ActionResult<IReadOnlyList<GameSummaryDto>>> Games(CancellationToken ct)
        => Ok(await _listings.GetGameSummariesAsync(ct));

    /// <summary>Kategoriyalar: e'lon turlari va o'yinlar, har biri sanog'i bilan.</summary>
    [HttpGet("categories")]
    public async Task<ActionResult<CategoriesDto>> Categories(CancellationToken ct)
        => Ok(await _listings.GetCategoriesAsync(ct));

    /// <summary>Bitta e'lon — to'liq ma'lumot va o'xshash e'lonlar.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ListingDetailDto>> Get(Guid id, CancellationToken ct)
        => Ok(await _listings.GetAsync(id, _user.Id, ct));

    /// <summary>Yangi e'lon joylashtirish.</summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ListingDetailDto>> Create(CreateListingRequest request, CancellationToken ct)
    {
        var created = await _listings.CreateAsync(_user.RequireId(), request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPatch("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ListingDetailDto>> Update(Guid id, UpdateListingRequest request, CancellationToken ct)
        => Ok(await _listings.UpdateAsync(id, _user.RequireId(), _user.IsModerator, request, ct));

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _listings.DeleteAsync(id, _user.RequireId(), _user.IsModerator, ct);
        return NoContent();
    }

    /// <summary>Skrinshot yuklash — javobda saqlangan rasm URL'i qaytadi.</summary>
    [HttpPost("upload")]
    [Authorize]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<object>> Upload(IFormFile file, CancellationToken ct)
    {
        _user.RequireId();

        if (file.Length == 0)
            throw new AppException("Fayl bo'sh.", 400, "empty_file");

        if (file.Length > _platform.MaxImageBytes)
            throw new AppException($"Rasm hajmi {_platform.MaxImageBytes / 1024 / 1024} MB dan oshmasin.",
                400, "file_too_large");

        await using var stream = file.OpenReadStream();
        var url = await _storage.SaveAsync(stream, file.FileName, file.ContentType, ct);

        return Ok(new { url });
    }
}
