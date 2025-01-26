using Domain.Enums;

namespace Domain.Entities
{
    public class Package
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public City City { get; set; }
        public CafeteriaLocation CafeteriaLocation { get; set; }
        public DateTime PickupDateTime { get; set; }
        public DateTime LastReservationDateTime { get; set; }
        public bool IsAdultOnly { get; private set; } // Make IsAdultOnly private set
        public decimal Price { get; set; }
        public MealType MealType { get; set; }
        public int CafeteriaId { get; set; }
        public Cafeteria Cafeteria { get; set; } = null!;
        public Reservation? Reservation { get; set; }
        public ICollection<Product> Products { get; set; } = new List<Product>();

        public void UpdateIsAdultOnly()
        {
            IsAdultOnly = Products.Any(p => p.IsAlcoholic);
        }
    }
}
