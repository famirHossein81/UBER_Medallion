using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using crud.Models;
using crud.Data;
using crud.DTOs;

namespace crud.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TripsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TripsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UberTrip>>> GetTrips([FromQuery] string? customerId, [FromQuery] int page = 1, [FromQuery] int pagesize = 50)
        {
            var query = _context.UberTrips.AsQueryable();

            if (!string.IsNullOrEmpty(customerId))
            {
                query = query.Where(t => t.CustomerId == customerId);
            }
            else
            {
            query = query.OrderByDescending(t => t.BookingDate);
            }
            var trips = await query.Skip((page -1) * pagesize).Take(pagesize).ToListAsync();

            return Ok(trips);
        }

        [HttpGet("{bookingId}")]
        public async Task<ActionResult<UberTrip>> GetTrip(string? bookingId)
        {
            var trip = await _context.UberTrips.FirstOrDefaultAsync(t => t.BookingId == bookingId);
            if (trip == null) return NotFound("No Trip Found");

            return Ok(trip);

        }

        [HttpPost]
        public async Task<ActionResult<UberTrip>> PostTrips([FromBody] UberTripDto input)
        {
            var booking_id = Guid.NewGuid().ToString();
            var tripDate = input.ManualDate.HasValue ? input.ManualDate.Value.ToUniversalTime() : DateTime.UtcNow;

            var new_trip = new UberTrip
            {
                BookingId = booking_id,
                BookingDate = tripDate.Date,
                BookingTime = tripDate.TimeOfDay,
                DayOfWeek = tripDate.DayOfWeek.ToString(),
                Hour = tripDate.Hour,
                PeriodOfTheDay = PeriodOfTheDay(tripDate.Hour),
                BookingStatus = "Completed",
                CustomerId = input.CustomerId,
                VehicleType = input.VehicleType,
                CancelledByCustomer = 0,
                CustomerCancelReason = null,
                CancelledByDriver = 0,
                DriverCancelReason = null,
                IncompleteRide = 0,
                IncompleteReason = null,
                BookingValue = input.BookingValue,
                RideDistance = input.RideDistance,
                PaymentMethod = input.PaymentMethod,
                DriverRating = null,
                CustomerRating = input.CustomerRating,
                HasDriverRating = 0,
                HasCustomerRating = input.CustomerRating != null ? 1 : 0,
                UnifiedCancellationReason = null,
                RevenuePerKm = input.RideDistance > 0 ? (input.BookingValue / input.RideDistance) : 0,
            };

            _context.UberTrips.Add(new_trip);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTrips), new { customerId = new_trip.CustomerId }, new_trip);
        }

        [HttpPut("{bookingId}")]
        public async Task<IActionResult> UpdateTrip(string bookingId, [FromBody] UpdateStatusDto input)
        {
            var trip = await _context.UberTrips.FirstOrDefaultAsync(t => t.BookingId == bookingId);
            if (trip == null) return NotFound("Trip not Found!");

            trip.BookingStatus = input.Status;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Status Updated Successfully" });
        }

        [HttpDelete("{bookingId}")]

        public async Task<IActionResult> DeleteTrip(string bookingId)
        {
            var trip = await _context.UberTrips.FirstOrDefaultAsync(t => t.BookingId == bookingId);
            if (trip == null) return NotFound("Trip not found");

            _context.UberTrips.Remove(trip);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private string PeriodOfTheDay(int hour)
        {
            if (hour >= 5 && hour < 12) return "Morning";
            if (hour >= 12 && hour < 18) return "Afternoon";
            if (hour >= 18 && hour < 23) return "Night";
            return "MidNight";
        }
    }
}