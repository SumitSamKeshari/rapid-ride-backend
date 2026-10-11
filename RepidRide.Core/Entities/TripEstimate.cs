using System;
using System.Collections.Generic;
using System.Text;

namespace RepidRide.Core.Entities
{
    public class TripEstimate
    {
        public double DistanceInKm { get; set; }
        public int DurationInMinutes { get; set; }
        public decimal EstimatedFare { get; set; }
        public string EncodedPolyline { get; set; } // Used to draw the blue line on the Flutter map
    }
}
