using Application.Common.Interfaces;
using Domain.Entities;
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
    private readonly IStudentService _studentService;

    public PackagesController(
        IPackageRepository packageRepository,
        IReservationRepository reservationRepository,
        IStudentService studentService)
    {
        _packageRepository = packageRepository;
        _reservationRepository = reservationRepository;
        _studentService = studentService;
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
        // Get the student from the identity claim
        var identityId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(identityId))
            return Unauthorized();

        // Get student from business database
        var student = await _studentService.GetStudentByIdentityIdAsync(identityId);
        if (student == null)
            return BadRequest("Student record not found");

        var package = await _packageRepository.GetByIdAsync(id);
        if (package == null)
            return NotFound();

        if (package.Reservation != null)
            return BadRequest("Package is already reserved");

        // Check no-show limit
        if (student.NoShowCount >= 2)
            return BadRequest("You cannot make reservations due to multiple no-shows");

        // Check age restriction
        if (package.IsAdultOnly && !student.IsOfLegalAge)
            return BadRequest("This package is restricted to users 18 and older");

        // Check existing reservation for the date
        if (await _reservationRepository.HasReservationForDateAsync(identityId, package.PickupDateTime.Date))
            return BadRequest("You already have a reservation for this date");

        var reservation = new Reservation
        {
            PackageId = package.Id,
            StudentNumber = student.StudentNumber,
            ReservationDateTime = DateTime.Now
        };

        await _reservationRepository.AddAsync(reservation);
        return Ok();
    }
}
