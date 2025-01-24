using Application.DTOs.Package;

namespace Application.DTOs.Packages
{
    public class PackageViewModel
    {
        public PackageDto Package { get; set; } = null!;
        public bool CanReserve { get; set; }
        public string? ReservationBlockReason { get; set; }
    }
}
