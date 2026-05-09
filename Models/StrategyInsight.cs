namespace Tradetool.Models
{
    public class StrategyInsight
    {
        public string Nome { get; set; } = string.Empty;
        public decimal Lucro { get; set; }
        public decimal ROI { get; set; }
        public int Trades { get; set; }
    }
}
