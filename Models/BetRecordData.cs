namespace Tradetool.Models
{
    public class BetRecordData
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public decimal? Stake { get; set; }
        public decimal? Responsability { get; set; }
        public decimal? PL { get; set; }
        public decimal? PLStake { get; set; }
        public string Home { get; set; }
        public string Away { get; set; }
        public string Competition { get; set; }
        public string Market { get; set; }
        public string Methods { get; set; }
        public string Side { get; set; }
        public decimal? Odds { get; set; }
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
        public string Comments { get; set; }
        public long BetId { get; set; }
        public decimal PercentualResultado { get; set; }
        public string? TargetTeam { get; set; }
        public string? Selection { get; set; }
    }
}
