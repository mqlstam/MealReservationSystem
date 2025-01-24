using Domain.Enums;

namespace Application.DTOs.Api;

// Application/DTOs/Api/PackageApiDto.cs
public class PackageApiDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public City City { get; set; }
    public CafeteriaLocation CafeteriaLocation { get; set; }
    public DateTime PickupDateTime { get; set; }
    public DateTime LastReservationDateTime { get; set; }
    public bool IsAdultOnly { get; set; }
    public decimal Price { get; set; }
    public MealType MealType { get; set; }
    public List<string> Products { get; set; } = new();
    public bool IsReserved { get; set; }
}

// Application/Common/Results/Result.cs
public class Result
{
    public bool Success { get; }
    public string? Error { get; }
    
    private Result(bool success, string? error = null)
    {
        Success = success;
        Error = error;
    }

    public static Result Ok() => new(true);
    public static Result Failure(string error) => new(false, error);
}