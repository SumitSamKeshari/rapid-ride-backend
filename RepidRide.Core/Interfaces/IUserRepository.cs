using RepidRide.Core.Entities;
using System.Threading.Tasks;

namespace RepidRide.Core.Interfaces
{
    public interface IUserRepository
    {
        Task<UserDto?> AuthenticateUserAsync(string email, string password);
    }
}