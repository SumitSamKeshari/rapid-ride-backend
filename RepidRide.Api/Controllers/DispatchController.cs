using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RepidRide.Api.Hubs;
using System;
using System.Threading.Tasks;

namespace RepidRide.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DispatchController : ControllerBase
    {
        private readonly IHubContext<RideHub> _hubContext;
        private readonly ILogger<DispatchController> _logger;

        public DispatchController(IHubContext<RideHub> hubContext, ILogger<DispatchController> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        [HttpPost("test-ping")]
        public async Task<IActionResult> PingDrivers()
        {
            try
            {
                var rideDetails = "🔥 TEST RIDE DISPATCH 🔥\n\nPickup: Ludhiana Junction Railway Station\nDropoff: Pavilion Mall, Civil Lines\nEst. Fare: ₹180\nDistance: 4.2 km";
                int testRideId = 999;
                int testRiderId = 100;

                _logger.LogInformation("Broadcasting test ping to AvailableDrivers group.");
                await _hubContext.Clients.Group("AvailableDrivers")
                    .SendAsync("ReceiveRideRequest", testRideId, testRiderId, rideDetails);

                return Ok(new { Message = "Ping successfully sent to connected drivers!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast test ping");
                return StatusCode(500, "Error sending test ping");
            }
        }

        [HttpPost("simulate-driver-move")]
        public async Task<IActionResult> SimulateDriverMove([FromBody] LocationUpdateRequest request)
        {
            try
            {
                string groupName = $"ActiveRide_{request.RideId}";
                _logger.LogInformation("Simulating move for Ride {RideId} to ({Lat}, {Lng})", 
                    request.RideId, request.Latitude, request.Longitude);

                await _hubContext.Clients.Group(groupName)
                    .SendAsync("ReceiveLocationUpdate", request.Latitude, request.Longitude);

                return Ok(new { Message = $"Successfully moved ride {request.RideId} to {request.Latitude}, {request.Longitude}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to simulate driver movement");
                return StatusCode(500, "Error simulating movement");
            }
        }
    }

    public class LocationUpdateRequest
    {
        public int RideId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}