using RepidRide.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RepidRide.Core.Interfaces
{
    public interface IRoutingService
    {
        Task<TripEstimate> GetTripEstimateAsync(LocationPoint pickup, LocationPoint dropoff);
    }
}
