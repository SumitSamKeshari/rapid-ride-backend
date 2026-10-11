using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using RepidRide.Core.Entities;
using RepidRide.Core.Interfaces;

namespace RepidRide.Services
{
    public class GoogleRoutingService : IRoutingService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        // We inject HttpClient to make the web request, and IConfiguration to read the API key and prices
        public GoogleRoutingService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<TripEstimate> GetTripEstimateAsync(LocationPoint pickup, LocationPoint dropoff)
        {
            string apiKey = _config["RoutingSettings:GoogleMapsApiKey"];

            // Build the Google Maps Directions API URL
            string url = $"https://maps.googleapis.com/maps/api/directions/json?origin={pickup.Latitude},{pickup.Longitude}&destination={dropoff.Latitude},{dropoff.Longitude}&key={apiKey}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return null; // Handle error appropriately in production
            }

            var jsonString = await response.Content.ReadAsStringAsync();

            // Parse the JSON. (Using JsonDocument is highly memory-efficient for reading nested external APIs)
            using (JsonDocument doc = JsonDocument.Parse(jsonString))
            {
                var root = doc.RootElement;
                if (root.GetProperty("status").GetString() != "OK")
                {
                    return null;
                }

                var route = root.GetProperty("routes")[0];
                var leg = route.GetProperty("legs")[0];

                // Google returns distance in meters and duration in seconds
                double distanceMeters = leg.GetProperty("distance").GetProperty("value").GetDouble();
                int durationSeconds = leg.GetProperty("duration").GetProperty("value").GetInt32();
                string polyline = route.GetProperty("overview_polyline").GetProperty("points").GetString();

                double distanceKm = distanceMeters / 1000.0;
                int durationMinutes = durationSeconds / 60;

                // Execute the Pricing Algorithm
                decimal fare = CalculateFare(distanceKm, durationMinutes);

                return new TripEstimate
                {
                    DistanceInKm = Math.Round(distanceKm, 2),
                    DurationInMinutes = durationMinutes,
                    EstimatedFare = fare,
                    EncodedPolyline = polyline
                };
            }
        }

        private decimal CalculateFare(double distanceKm, int durationMinutes)
        {
            decimal baseFare = Convert.ToDecimal(_config["RoutingSettings:BaseFare"]);
            decimal costPerKm = Convert.ToDecimal(_config["RoutingSettings:CostPerKm"]);
            decimal costPerMinute = Convert.ToDecimal(_config["RoutingSettings:CostPerMinute"]);

            decimal total = baseFare + (Convert.ToDecimal(distanceKm) * costPerKm) + (durationMinutes * costPerMinute);

            return Math.Round(total, 2);
        }
    }
}

