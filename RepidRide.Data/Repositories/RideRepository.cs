using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using RepidRide.Core.Interfaces;
using System;
using System.Data;
using System.Threading.Tasks;

namespace RepidRide.Data.Repositories
{
    public class RideRepository : IRideRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<RideRepository>? _logger;

        public RideRepository(string connectionString, ILogger<RideRepository>? logger = null)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger;
        }

        public async Task<int> CreateRideRequestAsync(int riderId, double pickupLat, double pickupLng)
        {
            try
            {
                using IDbConnection connection = new SqlConnection(_connectionString);
                var parameters = new { RiderId = riderId, PickupLatitude = pickupLat, PickupLongitude = pickupLng };

                var rideId = await connection.QuerySingleAsync<int>(
                    "sp_CreateRideRequest",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                _logger?.LogInformation("Ride request created successfully: RideId {RideId} for Rider {RiderId}", rideId, riderId);
                return rideId;
            }
            catch (SqlException ex)
            {
                _logger?.LogError(ex, "SQL error creating ride request for Rider {RiderId}", riderId);
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error creating ride request for Rider {RiderId}", riderId);
                throw;
            }
        }

        public async Task<bool> AcceptRideAsync(int rideId, int driverId)
        {
            try
            {
                using IDbConnection connection = new SqlConnection(_connectionString);
                var parameters = new { RideId = rideId, DriverId = driverId };

                var result = await connection.ExecuteScalarAsync<int>(
                    "sp_AcceptRide",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                bool success = result == 1;
                if (success)
                {
                    _logger?.LogInformation("Driver {DriverId} successfully accepted Ride {RideId}", driverId, rideId);
                }
                else
                {
                    _logger?.LogWarning("Driver {DriverId} failed to accept Ride {RideId} (already taken or invalid)", driverId, rideId);
                }

                return success;
            }
            catch (SqlException ex)
            {
                _logger?.LogError(ex, "SQL error in AcceptRideAsync for Ride {RideId}, Driver {DriverId}", rideId, driverId);
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error in AcceptRideAsync for Ride {RideId}, Driver {DriverId}", rideId, driverId);
                return false;
            }
        }

        public async Task<bool> CompleteRideAsync(int rideId, int driverId)
        {
            try
            {
                using IDbConnection connection = new SqlConnection(_connectionString);
                var parameters = new { RideId = rideId, DriverId = driverId };

                var result = await connection.ExecuteScalarAsync<int>(
                    "sp_CompleteRide",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                bool success = result == 1;
                if (success)
                {
                    _logger?.LogInformation("Ride {RideId} completed successfully by Driver {DriverId}", rideId, driverId);
                }
                else
                {
                    _logger?.LogWarning("Failed to complete Ride {RideId} by Driver {DriverId}", rideId, driverId);
                }

                return success;
            }
            catch (SqlException ex)
            {
                _logger?.LogError(ex, "SQL error in CompleteRideAsync for Ride {RideId}, Driver {DriverId}", rideId, driverId);
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error in CompleteRideAsync for Ride {RideId}, Driver {DriverId}", rideId, driverId);
                return false;
            }
        }
    }
}