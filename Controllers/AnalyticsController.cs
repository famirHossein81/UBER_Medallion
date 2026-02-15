using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using crud.Data;
using crud.DTOs;
using crud.Models;


namespace crud.Controllers
{
    [ApiController]
    [Route("api/{Controller}")]

    public class AnalyticsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AnalyticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private IQueryable<UberTrip> ApplyFilters(IQueryable<UberTrip> query, DateTime? start, DateTime? end, string? vehicleType)
        {
            if (start.HasValue)
            {
                query = query.Where(t => t.BookingDate >= start);
            }
            if (end.HasValue)
            {
                query = query.Where(t => t.BookingDate <= end);
            }
            if (!string.IsNullOrEmpty(vehicleType))
            {
                query = query.Where(t => t.VehicleType == vehicleType);
            }
            return query;
        }

        [HttpGet("kpis")]

        public async Task<ActionResult<DashboardKpiDto>> GetKpis([FromQuery] DateTime? start, [FromQuery] DateTime? end, [FromQuery] string? vehicleType)
        {
            var query = _context.UberTrips.AsQueryable();
            query = ApplyFilters(query, start, end, vehicleType);

            var total = await query.CountAsync();
            var successfull = await query.CountAsync(t => t.BookingStatus == "Completed");

            var revenue = await query.Where(t => t.BookingStatus == "Completed").SumAsync(t => t.BookingValue);

            return Ok(new DashboardKpiDto
            {
                TotalBookings = total,
                SuccessfulBookings = successfull,
                TotalRevenue = revenue,
                SuccessRate = total > 0 ? Math.Round((double)successfull / total * 100, 2) : 0
            });

        }

        [HttpGet("charts/cancellations")]
        public async Task<ActionResult<IEnumerable<ChartDataDto>>> GetCancellationDistribution(
            [FromQuery] DateTime? start, 
            [FromQuery] DateTime? end, 
            [FromQuery] string? vehicleType)
        {
            var query = _context.UberTrips.AsQueryable();
            query = ApplyFilters(query, start, end, vehicleType);

            var data = await query
                .Where(t => t.BookingStatus != "Completed") 
                .GroupBy(t => t.UnifiedCancellationReason ?? "Unknown")
                .Select(g => new ChartDataDto
                {
                    Label = g.Key,
                    Value = g.Count()
                })
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("charts/payment-methods")]
        public async Task<ActionResult<IEnumerable<ChartDataDto>>> GetPaymentDistribution( [FromQuery] DateTime? start, 
            [FromQuery] DateTime? end, 
            [FromQuery] string? vehicleType)
        {
            var query = _context.UberTrips.AsQueryable();
            query = ApplyFilters(query, start, end, vehicleType);

            var data = await query.GroupBy(t => t.PaymentMethod).Select(g => new ChartDataDto
            {
                Label = g.Key,
                Value = g.Count()
            }).ToListAsync();

            return Ok(data);
        }

        [HttpGet("charts/vehicles")]
        public async Task<ActionResult<IEnumerable<VehicleStatsDto>>> GetVehicleStats(
            [FromQuery] DateTime? start, 
            [FromQuery] DateTime? end)
        {
            var query = _context.UberTrips.AsQueryable();
            query = ApplyFilters(query, start, end, null); 

            var data = await query
                .Where(t => t.BookingStatus == "Completed")
                .GroupBy(t => t.VehicleType)
                .Select(g => new VehicleStatsDto
                {
                    VehicleType = g.Key,
                    TripCount = g.Count(),
                    AverageRating = g.Average(t => (double?)t.CustomerRating) ?? 0
                })
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("charts/traffic-hourly")]
        public async Task<ActionResult<IEnumerable<ChartDataDto>>> GetHourlyTraffic(
            [FromQuery] DateTime? start, 
            [FromQuery] DateTime? end,
            [FromQuery] string? vehicleType)
        {
            var query = _context.UberTrips.AsQueryable();
            query = ApplyFilters(query, start, end, vehicleType);

            var data = await query
                .GroupBy(t => t.Hour)
                .OrderBy(g => g.Key)
                .Select(g => new ChartDataDto
                {
                    Label = g.Key.ToString() + ":00", 
                    Value = g.Count()
                })
                .ToListAsync();

            return Ok(data);
        }
    }
}