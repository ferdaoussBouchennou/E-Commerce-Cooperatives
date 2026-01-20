namespace E_Commerce_Cooperatives.Models.ViewModels
{
    public class UserStatisticsViewModel
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int NewUsers { get; set; }
        public int ActiveThisPeriod { get; set; }
        public double ActivityRate { get; set; }
    }
}

