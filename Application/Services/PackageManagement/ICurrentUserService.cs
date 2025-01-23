namespace Application.Services.PackageManagement
{
    /// <summary>
    /// Application layer interface to retrieve current user info or cafeteria location.
    /// This is implemented in Infrastructure using UserManager<ApplicationUser>, 
    /// so that the Application layer does NOT depend on the Infrastructure.
    /// </summary>
    public interface ICurrentUserService
    {
        Task<string?> GetCafeteriaLocationAsync(string userId);
        Task<string?> GetFullNameAsync(string userId);
    }
}
