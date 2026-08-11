using Jetar.Domain.Common;
using Jetar.Application.Contracts;
using Jetar.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jetar.API.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;
    private readonly IListingService _listings;
    private readonly ICurrentUser _current;

    public UsersController(IUserService users, IListingService listings, ICurrentUser current)
    {
        _users = users;
        _listings = listings;
        _current = current;
    }

    /// <summary>Ommaviy profil.</summary>
    [HttpGet("{username}")]
    public async Task<ActionResult<PublicProfileDto>> Profile(string username, CancellationToken ct)
        => Ok(await _users.GetPublicProfileAsync(username, ct));

    /// <summary>Mening e'lonlarim — barcha holatlar bilan.</summary>
    [HttpGet("me/listings")]
    [Authorize]
    public async Task<ActionResult<PagedResult<ListingCardDto>>> MyListings(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 24, CancellationToken ct = default)
        => Ok(await _listings.SearchAsync(
            new ListingQuery { SellerId = _current.RequireId(), Page = page, PageSize = pageSize }, ct));

    [HttpPatch("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> UpdateProfile(UpdateProfileRequest request, CancellationToken ct)
        => Ok(await _users.UpdateProfileAsync(_current.RequireId(), request, ct));

    [HttpPatch("me/notifications")]
    [Authorize]
    public async Task<ActionResult<UserDto>> UpdateNotifications(NotificationSettingsRequest request, CancellationToken ct)
        => Ok(await _users.UpdateNotificationsAsync(_current.RequireId(), request, ct));

    /// <summary>Profil sahifasidagi umumiy ko'rsatkichlar.</summary>
    [HttpGet("me/summary")]
    [Authorize]
    public async Task<ActionResult<UserSummaryDto>> Summary(CancellationToken ct)
        => Ok(await _users.GetSummaryAsync(_current.RequireId(), ct));
}
