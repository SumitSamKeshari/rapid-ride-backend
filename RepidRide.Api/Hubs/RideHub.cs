using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RepidRide.Core.Interfaces;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RepidRide.Api.Hubs
{
    [Authorize]
    public class RideHub : Hub
    {
        private readonly IRideRepository _rideRepo;
        private readonly ILogger<RideHub> _logger;

        public RideHub(IRideRepository rideRepo, ILogger<RideHub> logger)
        {
            _rideRepo = rideRepo;
            _logger = logger;
        }

        private int? GetCurrentUserId()
        {
            var idClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? Context.User?.FindFirst("sub")?.Value
                       ?? Context.UserIdentifier;
            return int.TryParse(idClaim, out int id) ? id : null;
        }

        // ==========================================================
        // 1. CONNECTION & POOLING
        // ==========================================================
        public override async Task OnConnectedAsync()
        {
            try
            {
                var userId = GetCurrentUserId();
                var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

                if (role == "Driver")
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, "AvailableDrivers");
                }
                else if (role == "Rider" && userId.HasValue)
                {
                    // Add rider to their private notification room
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"Rider_{userId.Value}");
                }

                _logger.LogInformation("SignalR Client Connected: ConnectionId {ConnectionId}, UserId {UserId}, Role {Role}",
                    Context.ConnectionId, userId, role);

                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during SignalR OnConnectedAsync for connection {ConnectionId}", Context.ConnectionId);
                throw;
            }
        }

        // ==========================================================
        // 2. THE MATCHMAKING ENGINE
        // ==========================================================

        // STEP A: Rider broadcasts a request to all drivers
        [Authorize(Roles = "Rider")]
        public async Task RequestRide(double lat, double lng)
        {
            try
            {
                int? riderId = GetCurrentUserId();
                if (!riderId.HasValue)
                {
                    _logger.LogWarning("RequestRide called by unauthenticated or invalid user identity: {UserId}", Context.UserIdentifier);
                    await Clients.Caller.SendAsync("RideError", "Unauthorized user identity.");
                    return;
                }

                int newRideId = await _rideRepo.CreateRideRequestAsync(riderId.Value, lat, lng);
                string message = $"🚖 NEW FARE!\nRider #{riderId.Value} requested pickup at ({lat:F4}, {lng:F4})";

                _logger.LogInformation("Ride #{RideId} requested by Rider #{RiderId}", newRideId, riderId.Value);

                // Alert all available drivers with both the rideId and riderId
                await Clients.Group("AvailableDrivers").SendAsync("ReceiveRideRequest", newRideId, riderId.Value, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing RequestRide for coordinates ({Lat}, {Lng})", lat, lng);
                await Clients.Caller.SendAsync("RideError", "Unable to create ride request. Please try again.");
            }
        }

        // STEP B: A Driver clicks "Accept"
        [Authorize(Roles = "Driver")]
        public async Task AcceptRide(int rideId, int riderId)
        {
            try
            {
                int? driverId = GetCurrentUserId();
                if (!driverId.HasValue)
                {
                    _logger.LogWarning("AcceptRide called by invalid driver identity: {UserId}", Context.UserIdentifier);
                    await Clients.Caller.SendAsync("RideError", "Unauthorized driver identity.");
                    return;
                }

                bool success = await _rideRepo.AcceptRideAsync(rideId, driverId.Value);

                if (success)
                {
                    string groupName = $"ActiveRide_{rideId}";
                    await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

                    _logger.LogInformation("Match Successful: Driver #{DriverId} accepted Ride #{RideId}", driverId.Value, rideId);

                    // Notify ONLY the rider who made this request!
                    await Clients.Group($"Rider_{riderId}").SendAsync("RideAccepted", rideId, driverId.Value);

                    // Confirm acceptance to caller
                    await Clients.Caller.SendAsync("RideClaimSuccess", rideId);
                }
                else
                {
                    _logger.LogInformation("Ride #{RideId} already claimed or invalid. Notifying Driver #{DriverId}", rideId, driverId.Value);
                    await Clients.Caller.SendAsync("RideError", "This ride has already been claimed by another driver.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing AcceptRide for RideId {RideId}", rideId);
                await Clients.Caller.SendAsync("RideError", "Error accepting ride. Please try again.");
            }
        }

        // ==========================================================
        // 3. LIVE TRACKING
        // ==========================================================

        [Authorize(Roles = "Driver")]
        public async Task UpdateLocation(int rideId, double lat, double lng)
        {
            try
            {
                string groupName = $"ActiveRide_{rideId}";
                await Clients.Group(groupName).SendAsync("ReceiveLocationUpdate", lat, lng);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateLocation for Ride #{RideId}", rideId);
            }
        }

        [Authorize(Roles = "Rider")]
        public async Task JoinRideGroup(int rideId)
        {
            try
            {
                string groupName = $"ActiveRide_{rideId}";
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                _logger.LogInformation("Rider {UserId} joined tracking room for Ride #{RideId}", Context.UserIdentifier, rideId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JoinRideGroup for Ride #{RideId}", rideId);
            }
        }

        [Authorize(Roles = "Driver")]
        public async Task CompleteRide(int rideId)
        {
            try
            {
                int? driverId = GetCurrentUserId();
                if (!driverId.HasValue) return;

                bool success = await _rideRepo.CompleteRideAsync(rideId, driverId.Value);

                if (success)
                {
                    _logger.LogInformation("Ride #{RideId} completed by Driver #{DriverId}", rideId, driverId.Value);

                    string groupName = $"ActiveRide_{rideId}";
                    await Clients.Group(groupName).SendAsync("RideCompleted", rideId);
                }
                else
                {
                    _logger.LogWarning("Failed to complete Ride #{RideId} for Driver #{DriverId}", rideId, driverId.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CompleteRide for Ride #{RideId}", rideId);
            }
        }
    }
}