using System.Threading.Tasks;

namespace RepidRide.Core.Interfaces
{
    public interface ILocationRepository
    {
        void UpsertDriverLocation(int driverId, double lat, double lng);
        Task UpsertDriverLocationAsync(int driverId, double lat, double lng);
    }
}
