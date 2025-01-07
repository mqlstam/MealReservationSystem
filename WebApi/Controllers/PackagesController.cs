using Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.DTOs;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PackagesController : ControllerBase
{
    private readonly IPackageRepository _packageRepository;
    private readonly IReservationRepository _reservationRepository;

    public PackagesController(
        IPackageRepository packageRepository,
        IReservationRepository reservationRepository)
    {
        _packageRepository = packageRepository;
        _reservationRepository = reservationRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PackageDto>>> GetAvailablePackages()
    {
        var packages = await _packageRepository.GetAvailablePackagesAsync();
        
        var dtos = packages.Select(p => new PackageDto
        {
            Id = p.Id,
            Name = p.Name,
            City = p.City,
            CafeteriaLocation = p.CafeteriaLocation,
            PickupDateTime = p.PickupDateTime,
            LastReservationDateTime = p.LastReservationDateTime,
            IsAdultOnly = p.IsAdultOnly,
            Price = p.Price,
            MealType = p.MealType,
            ExampleProducts = p.Products.Select(prod => prod.Name).ToList(),
            IsReserved = p.Reservation != null
        });

        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PackageDto>> GetPackage(int id)
    {
        var package = await _packageRepository.GetByIdAsync(id);
        
        if (package == null)
            return NotFound();

        var dto = new PackageDto
        {
            Id = package.Id,
            Name = package.Name,
            City = package.City,
            CafeteriaLocation = package.CafeteriaLocation,
            PickupDateTime = package.PickupDateTime,
            LastReservationDateTime = package.LastReservationDateTime,
            IsAdultOnly = package.IsAdultOnly,
            Price = package.Price,
            MealType = package.MealType,
            ExampleProducts = package.Products.Select(p => p.Name).ToList(),
            IsReserved = package.Reservation != null
        };

        return Ok(dto);
    }

    [HttpGet("location/{location}")]
    public async Task<ActionResult<IEnumerable<PackageDto>>> GetPackagesByLocation(string location)
    {
        if (!Enum.TryParse<Domain.Enums.CafeteriaLocation>(location, true, out var cafeteriaLocation))
            return BadRequest("Invalid location");

        var packages = await _packageRepository.GetByLocationAsync(cafeteriaLocation);
        
        var dtos = packages.Select(p => new PackageDto
        {
            Id = p.Id,
            Name = p.Name,
            City = p.City,
            CafeteriaLocation = p.CafeteriaLocation,
            PickupDateTime = p.PickupDateTime,
            LastReservationDateTime = p.LastReservationDateTime,
            IsAdultOnly = p.IsAdultOnly,
            Price = p.Price,
            MealType = p.MealType,
            ExampleProducts = p.Products.Select(prod => prod.Name).ToList(),
            IsReserved = p.Reservation != null
        });

        return Ok(dtos);
    }

    [Authorize]
    [HttpPost("{id}/reserve")]
    public async Task<IActionResult> ReservePackage(int id)
    {
        var package = await _packageRepository.GetByIdAsync(id);
        if (package == null)
            return NotFound();

        if (package.Reservation != null)
            return BadRequest("Package is already reserved");

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Check if user has reached the no-show limit
        var noShowCount = await _reservationRepository.GetNoShowCountAsync(userId);
        if (noShowCount >= 2)
            return BadRequest("You cannot make reservations due to multiple no-shows");

        // Check if user already has a reservation for this date
        if (await _reservationRepository.HasReservationForDateAsync(userId, package.PickupDateTime.Date))
            return BadRequest("You already have a reservation for this date");

        var reservation = new Domain.Entities.Reservation
        {
            PackageId = package.Id,
            StudentId = userId,
            ReservationDateTime = DateTime.Now
        };

        await _reservationRepository.AddAsync(reservation);
        return Ok();
    }
}
