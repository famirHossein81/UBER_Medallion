namespace crud.DTOs
{
    public class UberTripDto
    {
        public string CustomerId { get; set; }
        public string VehicleType { get; set; }
        public string? PaymentMethod { get; set; }
        public decimal? CustomerRating { get; set; }
        public decimal? BookingValue {get; set;}

        public decimal? RideDistance {get;set;}

        public DateTime? ManualDate {get;set;}
    }
}
