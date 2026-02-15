namespace crud.DTOs
{
    public class DashboardKpiDto
    {
        public int TotalBookings { get; set; }
        public int SuccessfulBookings { get; set; }
        public decimal? TotalRevenue { get; set; }
        public double SuccessRate { get; set; }
    }

    public class ChartDataDto
    {
        public string Label { get; set; } 
        public double Value { get; set; } 
    }

    public class VehicleStatsDto
    {
        public string VehicleType { get; set; }
        public int TripCount { get; set; }
        public double AverageRating { get; set; }
    }
}