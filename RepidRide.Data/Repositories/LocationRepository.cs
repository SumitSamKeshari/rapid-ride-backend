using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using RepidRide.Core.Interfaces;
using System;
using System.Data;
using System.Threading.Tasks;

namespace RepidRide.Data.Repositories
{
    public class LocationRepository : ILocationRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<LocationRepository>? _logger;

        public LocationRepository(string connectionString, ILogger<LocationRepository>? logger = null)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger;
        }

        public void UpsertDriverLocation(int driverId, double lat, double lng)
        {
            UpsertDriverLocationAsync(driverId, lat, lng).GetAwaiter().GetResult();
        }

        public async Task UpsertDriverLocationAsync(int driverId, double lat, double lng)
        {
            try
            {
                using IDbConnection connection = new SqlConnection(_connectionString);
                var parameters = new { DriverId = driverId, Lat = lat, Lng = lng };

                await connection.ExecuteAsync(
                    "sp_UpsertDriverLocation",
                    parameters,
                    commandType: CommandType.StoredProcedure);
            }
            catch (SqlException ex)
            {
                _logger?.LogError(ex, "SQL error updating location for Driver {DriverId} at ({Lat}, {Lng})", driverId, lat, lng);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error in UpsertDriverLocation for Driver {DriverId}", driverId);
            }
        }
    }
}
