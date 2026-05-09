namespace Tradetool.Models
{
    public class TeamBestMethod
    {
        public string Team { get; set; } = "";
        public string Method { get; set; } = "";
        public decimal Lucro { get; set; }
        public decimal ROI { get; set; }
        public int Trades { get; set; }
        public decimal Winrate { get; set; }
    }
}