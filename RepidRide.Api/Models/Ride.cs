using System;

namespace RepidRide.Api.Models
{
    public class Ride
    {
        public int Id { get; set; }

        // The users involved
        public int RiderId { get; set; }
        public int? DriverId { get; set; } // Nullable because it starts with no driver

        // Ride Details
        public double PickupLatitude { get; set; }
        public double PickupLongitude { get; set; }

        // Status tracking (e.g., "Requested", "Accepted", "Completed", "Cancelled")
        public string Status { get; set; } = "Requested";

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
    }
}