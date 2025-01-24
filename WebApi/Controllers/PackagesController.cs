using Application.Common.Interfaces;
using Application.DTOs.Package;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PackagesController : ControllerBase
{
    private readonly IPackageRepository _packageRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IStudentService _studentService;
    private readonly IMappingService _mappingService;

    public PackagesController(
        IPackageRepository packageRepository,
        IReservationRepository reservationRepository,
        IStudentService studentService,
        IMappingService mappingService)
    {
        _packageRepository = packageRepository;
        _reservationRepository = reservationRepository;
        _studentService = studentService;
        _mappingService = mappingService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PackageDto>>> GetAvailablePackages()
    {
        var packages = await _packageRepository.GetAvailablePackagesAsync();
        var dtos = packages.Select(p => _mappingService.MapToDto(p));
        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PackageDto>> GetPackage(int id)
    {
        var package = await _packageRepository.GetByIdAsync(id);
        if (package == null)
            return NotFound();

        var dto = _mappingService.MapToDto(package);
        return Ok(dto);
    }

    [HttpGet("location/{location}")]
    public async Task<ActionResult<IEnumerable<PackageDto>>> GetPackagesByLocation(string location)
    {
        if (!Enum.TryParse<Domain.Enums.CafeteriaLocation>(location, true, out var cafeteriaLocation))
            return BadRequest("Invalid location");

        var packages = await _packageRepository.GetByLocationAsync(cafeteriaLocation);
        var dtos = packages.Select(p => _mappingService.MapToDto(p));
        return Ok(dtos);
    }

    [Authorize]
    [HttpPost("{id}/reserve")]
    public async Task<IActionResult> ReservePackage(int id)
    {
        var identityId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(identityId))
            return Unauthorized();

        var student = await _studentService.GetStudentByIdentityIdAsync(identityId);
        if (student == null)
            return BadRequest("Student record not found");

        var package = await _packageRepository.GetByIdAsync(id);
        if (package == null)
            return NotFound();

        if (package.Reservation != null)
            return BadRequest("Package is already reserved");

        if (student.NoShowCount >= 2)
            return BadRequest("You cannot make reservations due to multiple no-shows");

        if (package.IsAdultOnly && !student.IsOfLegalAge)
            return BadRequest("This package is restricted to users 18 and older");

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
