using System;
using System.Collections.Generic;
using System.Text;

namespace RepidRide.Core.Entities
{
    public class DriverLocation
    {
        public int DriverId { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
