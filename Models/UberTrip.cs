using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace crud.Models
{
    [Table("cleaned_dataset", Schema = "gold")]
    public class UberTrip
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("record_id")]
        public int RecordId { get; set; }

        [Column("booking_id")]
        public string BookingId { get; set; }

        [Column("booking_date")]
        public DateTime BookingDate { get; set; }

        [Column("booking_time")]
        public TimeSpan BookingTime { get; set; }

        [Column("day_of_week")]
        public string DayOfWeek { get; set; }

        [Column("hour")]
        public int Hour { get; set; }

        [Column("period_of_the_day")]
        public string? PeriodOfTheDay { get; set; }

        [Column("booking_status")]
        public string? BookingStatus { get; set; }

        [Column("customer_id")]
        public string? CustomerId { get; set; }

        [Column("vehicle_type")]
        public string VehicleType { get; set; }

        [Column("cancelled_by_customer")]
        public int CancelledByCustomer { get; set; }

        [Column("customer_cancel_reason")]
        public string? CustomerCancelReason { get; set; }

        [Column("cancelled_by_driver")]
        public int CancelledByDriver { get; set; }

        [Column("driver_cancel_reason")]
        public string? DriverCancelReason { get; set; }

        [Column("incomplete_ride")]
        public int IncompleteRide { get; set; }

        [Column("incomplete_reason")]
        public string? IncompleteReason { get; set; }

        [Column("booking_value")]
        public decimal? BookingValue { get; set; }

        [Column("ride_distance")]
        public decimal? RideDistance { get; set; }

        [Column("payment_method")]
        public string? PaymentMethod { get; set; }

        [Column("driver_rating")]
        public decimal? DriverRating { get; set; }

        [Column("customer_rating")]
        public decimal? CustomerRating { get; set; }

        [Column("has_driver_rating")]
        public int HasDriverRating { get; set; }

        [Column("has_customer_rating")]
        public int HasCustomerRating { get; set; }

        [Column("unified_cancellation_reason")]
        public string? UnifiedCancellationReason { get; set; }

        [Column("revenue_per_km")]
        public decimal? RevenuePerKm { get; set; }
    }
}