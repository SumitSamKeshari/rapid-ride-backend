using System;
using System.Collections.Generic;
using System.Text;

namespace RepidRide.Core.Entities
{
    public class Ride
    {
        public int RideId { get; set; }
        public int RiderId { get; set; }
        public int? DriverId { get; set; }

        public decimal FareAmount { get; set; }

        public string RideStatus { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
