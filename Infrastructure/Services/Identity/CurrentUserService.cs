using Application.Services.PackageManagement;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Infrastructure.Services.Identity
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICafeteriaRepository _cafeteriaRepository;

        public CurrentUserService(UserManager<ApplicationUser> userManager, ICafeteriaRepository cafeteriaRepository)
        {
            _userManager = userManager;
            _cafeteriaRepository = cafeteriaRepository;
        }

        public async Task<string?> GetCafeteriaLocationAsync(string userId)
        {
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == userId);
            return user?.CafeteriaLocation;
        }

        public async Task<string?> GetFullNameAsync(string userId)
        {
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return null;
            return $"{user.FirstName} {user.LastName}";
        }
    }
}
