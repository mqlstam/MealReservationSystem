namespace Application.Common.Interfaces;

public interface IIdentityService
{
    Task<bool> IsInRoleAsync(string userId, string role);
    Task<bool> AuthorizeAsync(string userId, string policyName);
    Task<(bool Success, string UserId)> CreateUserAsync(string userName, string password);
    Task<bool> DeleteUserAsync(string userId);
}
