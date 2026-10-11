using System.Threading.Tasks;

namespace RepidRide.Core.Interfaces
{
    public interface IRideRepository
    {
        Task<int> CreateRideRequestAsync(int riderId, double pickupLat, double pickupLng);
        Task<bool> AcceptRideAsync(int rideId, int driverId);
        Task<bool> CompleteRideAsync(int rideId, int driverId);
    }
}