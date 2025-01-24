namespace Application.DTOs.Reservation
{
    public class ReservationListViewModel
    {
        public List<ReservationDto> Reservations { get; set; } = new();
        public int NoShowCount { get; set; }
    }
}
