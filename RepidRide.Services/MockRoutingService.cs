using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using RepidRide.Core.Entities;
using RepidRide.Core.Interfaces;

namespace RepidRide.Services
{
    public class MockRoutingService : IRoutingService
    {
        private readonly IConfiguration _config;

        public MockRoutingService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<TripEstimate> GetTripEstimateAsync(LocationPoint pickup, LocationPoint dropoff)
        {
            // 1. Simulate network delay (so your Flutter app shows a loading spinner realistically)
            await Task.Delay(800);

            // 2. Calculate straight-line distance and add 30% to simulate roads
            double straightLineKm = CalculateHaversineDistance(pickup, dropoff);
            double estimatedRoadDistanceKm = straightLineKm * 1.3;

            // 3. Assume an average city driving speed of 35 km/h to calculate minutes
            int estimatedMinutes = (int)Math.Ceiling((estimatedRoadDistanceKm / 35.0) * 60);

            // 4. Calculate the Fare using your appsettings.json pricing
            decimal fare = CalculateFare(estimatedRoadDistanceKm, estimatedMinutes);

            return new TripEstimate
            {
                DistanceInKm = Math.Round(estimatedRoadDistanceKm, 2),
                DurationInMinutes = estimatedMinutes > 0 ? estimatedMinutes : 1, // Minimum 1 minute
                EstimatedFare = fare,

                // This is a dummy encoded polyline that draws a small triangle. 
                // It prevents the Flutter map decoder from crashing.
                EncodedPolyline = "o}w|F_c}uO?a@?a@?a@"
            };
        }

        private decimal CalculateFare(double distanceKm, int durationMinutes)
        {
            decimal baseFare = Convert.ToDecimal(_config["RoutingSettings:BaseFare"]);
            decimal costPerKm = Convert.ToDecimal(_config["RoutingSettings:CostPerKm"]);
            decimal costPerMinute = Convert.ToDecimal(_config["RoutingSettings:CostPerMinute"]);

            decimal total = baseFare + (Convert.ToDecimal(distanceKm) * costPerKm) + (durationMinutes * costPerMinute);
            return Math.Round(total, 2);
        }

        // Standard formula to calculate distance between two GPS coordinates
        private double CalculateHaversineDistance(LocationPoint p1, LocationPoint p2)
        {
            var R = 6371; // Radius of the earth in km
            var dLat = Deg2Rad(p2.Latitude - p1.Latitude);
            var dLon = Deg2Rad(p2.Longitude - p1.Longitude);
            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(Deg2Rad(p1.Latitude)) * Math.Cos(Deg2Rad(p2.Latitude)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double Deg2Rad(double deg) => deg * (Math.PI / 180);
    }
}
