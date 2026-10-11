using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RepidRide.Core.Entities;
using RepidRide.Core.Interfaces;
using System;
using System.Threading.Tasks;

namespace RepidRide.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TripController : ControllerBase
    {
        private readonly IRoutingService _routingService;
        private readonly ILogger<TripController> _logger;

        public TripController(IRoutingService routingService, ILogger<TripController> logger)
        {
            _routingService = routingService;
            _logger = logger;
        }

        [HttpPost("estimate")]
        public async Task<IActionResult> GetEstimate([FromBody] EstimateRequest request)
        {
            if (request?.Pickup == null || request.Dropoff == null)
            {
                return BadRequest("Valid Pickup and Dropoff coordinates are required.");
            }

            try
            {
                _logger.LogInformation("Calculating estimate from ({PickupLat}, {PickupLng}) to ({DropLat}, {DropLng})",
                    request.Pickup.Latitude, request.Pickup.Longitude, request.Dropoff.Latitude, request.Dropoff.Longitude);

                var estimate = await _routingService.GetTripEstimateAsync(request.Pickup, request.Dropoff);

                if (estimate == null)
                {
                    _logger.LogWarning("Routing service returned null for requested coordinates.");
                    return BadRequest("Could not calculate a route for these coordinates.");
                }

                return Ok(estimate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating trip estimate");
                return StatusCode(500, "Error generating trip estimate. Please try again.");
            }
        }
    }

    public class EstimateRequest
    {
        public LocationPoint Pickup { get; set; } = new LocationPoint();
        public LocationPoint Dropoff { get; set; } = new LocationPoint();
    }
}
