using System.Collections.Generic;

namespace Tradetool.Models
{
    public class DashboardData
    {
        // ===== KPI PRINCIPAIS =====
        public decimal LucroTotal { get; set; }
        public decimal ROI { get; set; }
        public decimal Winrate { get; set; }
        public int TotalTrades { get; set; }
        public decimal LucroMedio { get; set; }

        // ===== MÉTRICAS PRO (TRADER) =====
        public decimal DrawdownMax { get; set; }
        public decimal ProfitFactor { get; set; }
        public decimal Expectancy { get; set; }
        public decimal Kelly { get; set; }

        // ===== GRÁFICO =====
        public List<SaldoEvolucao> Evolucao { get; set; } = new();

        // ===== TIMES =====
        public List<RankingItem> RankingTeams { get; set; } = new();
        public List<RankingItem> RankingTeamsLoss { get; set; } = new();

        // ===== COMPETIÇÕES =====
        public List<RankingItem> RankingCompetitions { get; set; } = new();
        public List<RankingItem> RankingCompetitionsLoss { get; set; } = new();

        // ===== MÉTODOS =====
        public List<RankingItem> RankingMethods { get; set; } = new();
        public List<RankingItem> RankingMethodsLoss { get; set; } = new();

        // ===== MÉTODO + ODDS =====
        public List<RankingItem> MethodsOdds { get; set; } = new();

        // ===== ODDS =====
        public List<OddsAnalysis> OddsAnalysis { get; set; } = new();
        public List<OddsAnalysis> OddsROI { get; set; } = new();

        // ===== ALERTA =====
        public bool AlertaPerda { get; set; }

        public List<TeamBestMethod> BestMethodsPerTeam { get; set; } = new();
    }

    public class SaldoEvolucao
    {
        public string Data { get; set; } = string.Empty;
        public decimal Saldo { get; set; }
    }

    public class RankingItem
    {
        public string Nome { get; set; } = string.Empty;
        public decimal Valor { get; set; }
    }

    public class OddsAnalysis
    {
        public string Range { get; set; } = string.Empty;
        public decimal Resultado { get; set; }
    }
}