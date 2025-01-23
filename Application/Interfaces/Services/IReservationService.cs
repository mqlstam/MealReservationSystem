using System.Threading.Tasks;

namespace Application.Common.Interfaces.Services
{
    /// <summary>
    /// Defines reservation-related operations to be used by controllers or other app layers.
    /// </summary>
    public interface IReservationService
    {
        /// <summary>
        /// Reserves the specified package by the given user if the user is eligible,
        /// and the package is not yet reserved, etc. Returns a success or error message.
        /// </summary>
        /// <param name="packageId">The ID of the package to reserve.</param>
        /// <param name="userId">The Identity ID of the user reserving.</param>
        /// <returns>Success or error message (for UI display).</returns>
        Task<string> ReservePackageAsync(int packageId, string userId);

         /// <summary>
        /// Marks the specified package's reservation as picked up.
        /// </summary>
        Task MarkAsPickedUpAsync(int packageId);

        /// <summary>
        /// Marks the specified package's reservation as a no-show,
        /// and increments the student's no-show count.
        /// </summary>
        Task MarkAsNoShowAsync(int packageId);
    }
}

