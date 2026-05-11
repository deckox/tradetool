using Microsoft.Data.Sqlite;
using Tradetool.Models;

namespace Tradetool.Core
{
    public class BetRecord : BaseRepository
    {
        #region SQL

        private const string Sql_Insert = @"
        INSERT INTO BetHistory 
        (Date, Stake, Responsability, PL, PLStake, Home, Away, Competition, Market, Methods, Side, Odds, Amount, BalanceAfter, Comments, BetId, TargetTeam)
        VALUES 
        (@Date, @Stake, @Responsability, @PL, @PLStake, @Home, @Away, @Competition, @Market, @Methods, @Side, @Odds, @Amount, @BalanceAfter, @Comments, @BetId, @TargetTeam)
        ON CONFLICT(BetId) DO NOTHING;";

        private const string Sql_Update = @"
        UPDATE BetHistory SET 
            Date = @Date,
            Stake = @Stake,
            Responsability = @Responsability,
            PL = @PL,
            PLStake = @PLStake,
            Home = @Home,
            Away = @Away,
            Competition = @Competition,
            Market = @Market,
            Methods = @Methods,
            Side = @Side,
            Odds = @Odds,
            Amount = @Amount,
            BalanceAfter = @BalanceAfter,
            Comments = @Comments,
            BetId = @BetId,
            TargetTeam = @TargetTeam
        WHERE Id = @Id";

        private const string Sql_Delete = "DELETE FROM BetHistory WHERE Id = @Id";
        private const string Sql_Select = "SELECT * FROM BetHistory";
        private const string Sql_SelectOne = "SELECT * FROM BetHistory WHERE Id = @Id";

        #endregion

        #region CRUD

        public bool Save(BetRecordData bet)
        {
            using var connection = GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = bet.Id > 0
                ? Sql_Update
                : Sql_Insert;

            if (bet.Id > 0)
                command.Parameters.AddWithValue("@Id", bet.Id);

            command.Parameters.AddWithValue("@Date", bet.Date);
            command.Parameters.AddWithValue("@Stake", bet.Stake ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Responsability", bet.Responsability ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@PL", bet.PL ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@PLStake", bet.PLStake ?? (object)DBNull.Value);

            command.Parameters.AddWithValue("@Home", bet.Home?.Trim() ?? "");
            command.Parameters.AddWithValue("@Away", bet.Away?.Trim() ?? "");
            command.Parameters.AddWithValue("@Competition", bet.Competition?.Trim() ?? "");
            command.Parameters.AddWithValue("@Market", bet.Market?.Trim() ?? "");
            command.Parameters.AddWithValue("@Methods", bet.Methods?.Trim() ?? "");
            command.Parameters.AddWithValue("@Side", bet.Side?.Trim() ?? "");

            command.Parameters.AddWithValue("@Odds", bet.Odds ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Amount", bet.Amount);
            command.Parameters.AddWithValue("@BalanceAfter", bet.BalanceAfter);
            command.Parameters.AddWithValue("@Comments", bet.Comments?.Trim() ?? "");
            command.Parameters.AddWithValue("@BetId", bet.BetId);
            command.Parameters.AddWithValue("@TargetTeam",bet.TargetTeam ?? "");

            return command.ExecuteNonQuery() > 0;
        }

        public bool Delete(int id)
        {
            using var connection = GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = Sql_Delete;
            command.Parameters.AddWithValue("@Id", id);

            return command.ExecuteNonQuery() > 0;
        }

        public BetRecordData? Load(int id)
        {
            using var connection = GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = Sql_SelectOne;
            command.Parameters.AddWithValue("@Id", id);

            using var reader = command.ExecuteReader();

            return reader.Read()
                ? Parse(reader)
                : null;
        }

        public List<BetRecordData> List()
        {
            using var connection = GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = Sql_Select;

            var result = new List<BetRecordData>();

            using var reader = command.ExecuteReader();

            while (reader.Read())
                result.Add(Parse(reader));

            return result;
        }

        #endregion

        #region TEAMS

        public List<string> GetAllTeams()
        {
            using var conn = GetConnection();
            conn.Open();

            var list = new List<string>();

            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
            SELECT DISTINCT Home FROM BetHistory
            UNION
            SELECT DISTINCT Away FROM BetHistory
            ORDER BY 1";

            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                if (!r.IsDBNull(0))
                    list.Add(r.GetString(0));
            }

            return list;
        }

        public List<SaldoEvolucao> GetEvolucaoPorTime(string team)
        {
            using var conn = GetConnection();
            conn.Open();

            var list = new List<SaldoEvolucao>();

            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
    WITH Trades AS (

        SELECT
            strftime('%Y-%m-%d %H:%M', Date) AS TradeDate,

            Home,
            Away,
            Market,
            Methods,

            ROUND(
                SUM(
                    CASE
                        WHEN Side = 'LAY'
                        THEN Stake
                        ELSE 0
                    END
                )
                -
                SUM(
                    CASE
                        WHEN Side = 'BACK'
                        THEN Stake
                        ELSE 0
                    END
                )
            ,2) AS Resultado

        FROM BetHistory

        WHERE
            TargetTeam LIKE @team

        GROUP BY
            TradeDate,
            Home,
            Away,
            Market,
            Methods
    )

    SELECT
        substr(TradeDate,1,10),

        ROUND(
            SUM(Resultado)
            OVER (ORDER BY TradeDate)
        ,2)

    FROM Trades;
    ";

            cmd.Parameters.AddWithValue(
                "@team",
                "%" + team + "%");

            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                list.Add(new SaldoEvolucao
                {
                    Data = r.GetString(0),

                    Saldo = r.IsDBNull(1)
                        ? 0
                        : r.GetDecimal(1)
                });
            }

            return list;
        }

        #endregion

        #region DASHBOARD

        public DashboardData GetDashboard(string? team = null, string? month = null)
        {
            using var connection = GetConnection();
            connection.Open();

            var data = new DashboardData();

            var filtro = BuildFilter(team, month);

            LoadKpis(connection, data, filtro);
            LoadEvolucao(connection, data, filtro);

            LoadRanking(
                connection,
                data.RankingTeams,
                "TargetTeam",
                filtro,
                true
            );

            LoadRanking(
                connection,
                data.RankingTeamsLoss,
                "TargetTeam",
                filtro,
                false
            );

            LoadRanking(
                connection,
                data.RankingCompetitions,
                "Competition",
                filtro,
                true
            );

            LoadRanking(
                connection,
                data.RankingCompetitionsLoss,
                "Competition",
                filtro,
                false
            );

            LoadRanking(
                connection,
                data.RankingMethods,
                "Methods",
                filtro + " AND Methods IS NOT NULL AND TRIM(Methods) <> ''",
                true
            );

            LoadRanking(
                connection,
                data.RankingMethodsLoss,
                "Methods",
                filtro + " AND Methods IS NOT NULL AND TRIM(Methods) <> ''",
                false
            );

            LoadMethodsOdds(connection, data, filtro);
            LoadOdds(connection, data, filtro);
            LoadOddsROI(connection, data, filtro);
            LoadBestMethodsPerTeam(connection, data);

            data.AlertaPerda = data.LucroTotal < 0;

            return data;
        }

        #endregion

        #region KPI

        private void LoadKpis(
     SqliteConnection connection,
     DashboardData data,
     string filtro)
        {
            using var cmd = connection.CreateCommand();

            cmd.CommandText = $@"

    WITH Trades AS
    (
        SELECT
            BetId,
            ROUND(SUM(Amount),2) AS Resultado

        FROM BetHistory

        {filtro}

        AND Methods <> 'COMMISSION'

        GROUP BY BetId
    )

    SELECT

        ROUND(SUM(Resultado),2),

        ROUND(
            (
                SUM(Resultado)
                /
                NULLIF(SUM(ABS(Resultado)),0)
            ) * 100
        ,2),

        ROUND(
            (
                SUM(
                    CASE
                        WHEN Resultado > 0
                        THEN 1
                        ELSE 0
                    END
                ) * 100.0
            ) / COUNT(*)
        ,2),

        COUNT(*),

        ROUND(AVG(Resultado),2)

    FROM Trades";

            using var r = cmd.ExecuteReader();

            if (!r.Read())
                return;

            data.LucroTotal =
                r.IsDBNull(0) ? 0 : r.GetDecimal(0);

            data.ROI =
                r.IsDBNull(1) ? 0 : r.GetDecimal(1);

            data.Winrate =
                r.IsDBNull(2) ? 0 : r.GetDecimal(2);

            data.TotalTrades =
                r.IsDBNull(3) ? 0 : r.GetInt32(3);

            data.LucroMedio =
                r.IsDBNull(4) ? 0 : r.GetDecimal(4);
        }

        #endregion

        #region EVOLUCAO

        private void LoadEvolucao(
            SqliteConnection connection,
            DashboardData data,
            string filtro)
        {
            using var cmd = connection.CreateCommand();

            cmd.CommandText = $@"
            WITH Trades AS (
                SELECT
                    strftime('%Y-%m-%d %H:%M', Date) AS Data,

                    ROUND(
                        SUM(CASE WHEN Side='LAY' THEN Stake ELSE 0 END) -
                        SUM(CASE WHEN Side='BACK' THEN Stake ELSE 0 END)
                    ,2) AS Resultado

                FROM BetHistory
                {filtro}

                GROUP BY Data, Home, Away, Market, Methods
            )

            SELECT
                Data,
                ROUND(SUM(Resultado) OVER (ORDER BY Data),2)

            FROM Trades";

            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                data.Evolucao.Add(new SaldoEvolucao
                {
                    Data = r.GetString(0),
                    Saldo = r.GetDecimal(1)
                });
            }
        }

        #endregion

        #region RANKING

        private void LoadRanking(
            SqliteConnection connection,
            List<RankingItem> list,
            string campo,
            string filtro,
            bool lucro)
        {
            using var cmd = connection.CreateCommand();

            var operador = lucro ? ">" : "<";
            var order = lucro ? "DESC" : "ASC";

            cmd.CommandText = $@"
            SELECT
                {campo},

                ROUND(
                    SUM(Amount)
                ,2)

            FROM BetHistory
            {filtro}

            GROUP BY {campo}

            HAVING SUM(
                CASE WHEN Side='LAY' THEN Stake ELSE 0 END -
                CASE WHEN Side='BACK' THEN Stake ELSE 0 END
            ) {operador} 0

            ORDER BY 2 {order}
            LIMIT 5";

            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                list.Add(new RankingItem
                {
                    Nome = r.IsDBNull(0) ? "" : r.GetString(0),
                    Valor = r.IsDBNull(1) ? 0 : r.GetDecimal(1)
                });
            }
        }

        #endregion

        #region METHODS ODDS

        private void LoadMethodsOdds(
    SqliteConnection connection,
    DashboardData data,
    string filtro)
        {
            using var cmd = connection.CreateCommand();

            cmd.CommandText = $@"

    WITH Trades AS
    (
        SELECT

            BetId,

            Methods,

            Odds,

            ROUND(SUM(Amount),2) AS Resultado

        FROM BetHistory

        {filtro}

        AND Methods IS NOT NULL
        AND TRIM(Methods) <> ''
        AND Methods <> 'COMMISSION'

        GROUP BY
            BetId,
            Methods,
            Odds
    )

    SELECT

        Methods || ' | ' ||

        CASE
            WHEN Odds < 2 THEN '1-2'
            WHEN Odds < 3 THEN '2-3'
            WHEN Odds < 5 THEN '3-5'
            ELSE '5+'
        END,

        ROUND(SUM(Resultado),2)

    FROM Trades

    GROUP BY
        Methods,

        CASE
            WHEN Odds < 2 THEN '1-2'
            WHEN Odds < 3 THEN '2-3'
            WHEN Odds < 5 THEN '3-5'
            ELSE '5+'
        END

    ORDER BY SUM(Resultado) DESC

    LIMIT 10";

            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                data.MethodsOdds.Add(new RankingItem
                {
                    Nome = r.IsDBNull(0)
                        ? ""
                        : r.GetString(0),

                    Valor = r.IsDBNull(1)
                        ? 0
                        : r.GetDecimal(1)
                });
            }
        }

        #endregion

        #region ODDS

        private void LoadOdds(
            SqliteConnection connection,
            DashboardData data,
            string filtro)
        {
            using var cmd = connection.CreateCommand();

            cmd.CommandText = $@"
            SELECT

                CASE
                    WHEN Odds < 2 THEN '1.0-1.99'
                    WHEN Odds < 3 THEN '2.0-2.99'
                    WHEN Odds < 5 THEN '3.0-4.99'
                    ELSE '5.0+'
                END,

                ROUND(
                    SUM(
                        CASE WHEN Side='LAY' THEN Stake ELSE 0 END -
                        CASE WHEN Side='BACK' THEN Stake ELSE 0 END
                    )
                ,2)

            FROM BetHistory
            {filtro}

            GROUP BY 1";

            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                data.OddsAnalysis.Add(new OddsAnalysis
                {
                    Range = r.GetString(0),
                    Resultado = r.GetDecimal(1)
                });
            }
        }

        private void LoadOddsROI(
            SqliteConnection connection,
            DashboardData data,
            string filtro)
        {
            using var cmd = connection.CreateCommand();

            cmd.CommandText = $@"
            SELECT

                CASE
                    WHEN Odds < 2 THEN '1.0-1.99'
                    WHEN Odds < 3 THEN '2.0-2.99'
                    WHEN Odds < 5 THEN '3.0-4.99'
                    ELSE '5.0+'
                END,

                ROUND(
                    SUM(
                        CASE WHEN Side='LAY' THEN Stake ELSE 0 END -
                        CASE WHEN Side='BACK' THEN Stake ELSE 0 END
                    )
                    /
                    NULLIF(SUM(ABS(Stake)),0)
                    * 100
                ,2)

            FROM BetHistory
            {filtro}

            GROUP BY 1";

            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                data.OddsROI.Add(new OddsAnalysis
                {
                    Range = r.GetString(0),
                    Resultado = r.GetDecimal(1)
                });
            }
        }

        #endregion

        #region BEST METHODS

        private void LoadBestMethodsPerTeam(
            SqliteConnection connection,
            DashboardData data)
        {
            using var cmd = connection.CreateCommand();

            cmd.CommandText = @"
            WITH TeamMethods AS (

                SELECT
                    Home AS Team,
                    Methods,

                    COUNT(*) AS Trades,

                    ROUND(
                        SUM(
                            CASE WHEN Side='LAY' THEN Stake ELSE 0 END -
                            CASE WHEN Side='BACK' THEN Stake ELSE 0 END
                        )
                    ,2) AS Lucro,

                    ROUND(
                        SUM(
                            CASE WHEN Side='LAY' THEN Stake ELSE 0 END -
                            CASE WHEN Side='BACK' THEN Stake ELSE 0 END
                        )
                        /
                        NULLIF(SUM(ABS(Stake)),0)
                        * 100
                    ,2) AS ROI,

                    ROUND(
                        SUM(
                            CASE
                                WHEN (
                                    CASE WHEN Side='LAY' THEN Stake ELSE 0 END -
                                    CASE WHEN Side='BACK' THEN Stake ELSE 0 END
                                ) > 0
                                THEN 1
                                ELSE 0
                            END
                        ) * 100.0 / COUNT(*)
                    ,2) AS Winrate

                FROM BetHistory

                WHERE Methods IS NOT NULL
                AND TRIM(Methods) <> ''

                GROUP BY Home, Methods
            ),

            Ranked AS (

                SELECT *,
                ROW_NUMBER() OVER (
                    PARTITION BY Team
                    ORDER BY ROI DESC
                ) AS rn

                FROM TeamMethods

                WHERE Trades >= 5
            )

            SELECT
                Team,
                Methods,
                Lucro,
                ROI,
                Trades,
                Winrate

            FROM Ranked

            WHERE rn = 1

            ORDER BY ROI DESC
            LIMIT 20";

            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                data.BestMethodsPerTeam.Add(new TeamBestMethod
                {
                    Team = r.GetString(0),
                    Method = r.GetString(1),
                    Lucro = r.GetDecimal(2),
                    ROI = r.GetDecimal(3),
                    Trades = r.GetInt32(4),
                    Winrate = r.GetDecimal(5)
                });
            }
        }

        #endregion

        #region HELPERS

        private string BuildFilter(string? team, string? month)
        {
            var filtro = "WHERE 1=1";

            if (!string.IsNullOrEmpty(team))
            {
                filtro +=
                    $" AND TargetTeam = '{team}'";
            }

            if (!string.IsNullOrEmpty(month))
            {
                filtro +=
                    $" AND strftime('%Y-%m', Date) = '{month}'";
            }

            return filtro;
        }

        public List<BetRecordData> GetHistory(
    string? period = null,
    string? startDate = null,
    string? endDate = null,
    string? month = null)
        {
            using var connection = GetConnection();
            connection.Open();

            var sql = @"

    WITH Trades AS (

        SELECT

            MAX(Id) AS Id,

            MAX(Date) AS Date,

            TargetTeam,

            Home,
            Away,

            MAX(Competition) AS Competition,

            MAX(Market) AS Market,

            MAX(Methods) AS Methods,

            MAX(Odds) AS Odds,

            ROUND(
                MAX(
                    CASE
                        WHEN Side = 'LAY'
                        THEN Responsability
                        ELSE 0
                    END
                )
            ,2) AS Responsabilidade,

            ROUND(
                (
                    MAX(
                        CASE
                            WHEN Side = 'LAY'
                            THEN Responsability
                            ELSE 0
                        END
                    )
                )
                /
                NULLIF(
                    MAX(Odds) - 1,
                    0
                )
            ,2) AS StakeEntrada,

            ROUND(
                SUM(
                    CASE
                        WHEN Side = 'LAY'
                        THEN Stake
                        ELSE 0
                    END
                )
                -
                SUM(
                    CASE
                        WHEN Side = 'BACK'
                        THEN Stake
                        ELSE 0
                    END
                )
            ,2) AS Lucro

        FROM BetHistory

        WHERE 1=1
    ";

            using var cmd = connection.CreateCommand();

            if (period == "week")
            {
                sql += " AND Date >= date('now', '-7 day')";
            }

            if (!string.IsNullOrEmpty(month))
            {
                sql += " AND strftime('%Y-%m', Date) = @month";

                cmd.Parameters.AddWithValue(
                    "@month",
                    month);
            }

            if (!string.IsNullOrEmpty(startDate))
            {
                sql += " AND Date >= @startDate";

                cmd.Parameters.AddWithValue(
                    "@startDate",
                    startDate);
            }

            if (!string.IsNullOrEmpty(endDate))
            {
                sql += " AND Date <= @endDate";

                cmd.Parameters.AddWithValue(
                    "@endDate",
                    endDate + " 23:59:59");
            }

            sql += @"

        GROUP BY
            strftime('%Y-%m-%d %H:%M', Date),
            TargetTeam,
            Home,
            Away,
            Market,
            Methods
    )

    SELECT *
    FROM Trades
    WHERE Lucro <> 0
    ORDER BY Date DESC;
    ";

            cmd.CommandText = sql;

            var list = new List<BetRecordData>();

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var responsabilidade =
                    reader["Responsabilidade"] != DBNull.Value
                        ? Convert.ToDecimal(reader["Responsabilidade"])
                        : 0;

                var lucro =
                    reader["Lucro"] != DBNull.Value
                        ? Convert.ToDecimal(reader["Lucro"])
                        : 0;

                decimal roi = 0;

                if (responsabilidade > 0)
                {
                    roi = Math.Round(
                        (lucro / responsabilidade) * 100,
                        2);
                }

                list.Add(new BetRecordData
                {
                    Id = Convert.ToInt32(reader["Id"]),

                    Date = Convert.ToDateTime(reader["Date"]),

                    TargetTeam = reader["TargetTeam"]?.ToString(),

                    Home = reader["Home"]?.ToString(),

                    Away = reader["Away"]?.ToString(),

                    Competition = reader["Competition"]?.ToString(),

                    Market = reader["Market"]?.ToString(),

                    Methods = reader["Methods"]?.ToString(),

                    Side = "LAY",

                    Odds = reader["Odds"] != DBNull.Value
                        ? Convert.ToDecimal(reader["Odds"])
                        : null,

                    Stake = reader["StakeEntrada"] != DBNull.Value
                        ? Convert.ToDecimal(reader["StakeEntrada"])
                        : null,

                    Responsability = responsabilidade,

                    PL = lucro,

                    PLStake = roi
                });
            }

            return list;
        }

        private BetRecordData Parse(SqliteDataReader reader)
        {
            return new BetRecordData
            {
                Id = Convert.ToInt32(reader["Id"]),
                Date = Convert.ToDateTime(reader["Date"]),
                Stake = reader["Stake"] != DBNull.Value ? Convert.ToDecimal(reader["Stake"]) : null,
                Responsability = reader["Responsability"] != DBNull.Value ? Convert.ToDecimal(reader["Responsability"]) : null,
                PL = reader["PL"] != DBNull.Value ? Convert.ToDecimal(reader["PL"]) : null,
                PLStake = reader["PLStake"] != DBNull.Value ? Convert.ToDecimal(reader["PLStake"]) : null,
                Home = reader["Home"]?.ToString(),
                Away = reader["Away"]?.ToString(),
                Competition = reader["Competition"]?.ToString(),
                Market = reader["Market"]?.ToString(),
                Methods = reader["Methods"]?.ToString(),
                Side = reader["Side"]?.ToString(),
                Odds = reader["Odds"] != DBNull.Value ? Convert.ToDecimal(reader["Odds"]) : null,
                Amount = Convert.ToDecimal(reader["Amount"]),
                BalanceAfter = Convert.ToDecimal(reader["BalanceAfter"]),
                Comments = reader["Comments"]?.ToString(),
                BetId = reader["BetId"] != DBNull.Value ? Convert.ToInt32(reader["BetId"]) : 0,
                TargetTeam = reader["TargetTeam"]?.ToString()
            };
        }

        #endregion
    }
}