namespace Tradetool.Models
{
    public class BetRegister
    {
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public string System { get; set; }
        public string BetType { get; set; }
        public long BetId { get; set; }
        public long MarketId { get; set; }
        public decimal? Stake { get; set; }
        public decimal? Odds { get; set; }
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
    }
}
