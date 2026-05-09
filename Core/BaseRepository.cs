using Microsoft.Data.Sqlite;

namespace Tradetool.Core
{
    public class BaseRepository
    {
        private const string DatabaseFile = "banco.db";
        private readonly string _connectionString;
        private readonly string _databasePath;
        private const string Sql_CreateUniqueIndex = @"
        CREATE UNIQUE INDEX IF NOT EXISTS idx_bet_unique 
        ON BetHistory (BetId);";

        private const string Sql_CreateBetRecord = @"
        CREATE TABLE IF NOT EXISTS BetHistory (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Date TEXT,                 -- Data da aposta
            Stake REAL,                -- Stake da aposta     
            Responsability REAL,       -- Stake da aposta em lay (0 para back, valor do lay para lay)
            PL REAL,                   -- Profit/Loss da aposta
            PLStake REAL,              -- Profit/Loss multiplicado pelo Stake (PL * Stake)
            Home TEXT,                 -- Jogo (ex: Manchester United vs Brentford)
            Away TEXT,                 -- Jogo (ex: Manchester United vs Brentford)
            Competition TEXT,          -- Liga (extraído da descrição)
            Market TEXT,               -- Mercado (ex: Correct Score)
            Methods TEXT,              -- Seleção (ex: 0-1, ANY OTHER HOME WIN)
            Side TEXT,                 -- Side (ex: Back, Lay)
            Odds REAL,                 -- Odds da aposta
            Amount REAL,               -- Valor da aposta (positivo para ganhos, negativo para perdas)
            BalanceAfter REAL,         -- Saldo após a aposta
            Comments TEXT,              -- Comentários adicionais (extraído da descrição, como Red Card, In-Play, etc.)
            BetId INTEGER              -- Número da aposta (extraído do BetId, para referência futura e evitar duplicidade
        );";

        public BaseRepository()
        {
            var folder = Path.Combine(AppContext.BaseDirectory, "App_Data");

            // Garante que a pasta existe
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            _databasePath = Path.Combine(folder, DatabaseFile);
            _connectionString = $"Data Source={_databasePath}";

            InitializeDatabase();
        }

        public SqliteConnection GetConnection()
        {
            return new SqliteConnection(_connectionString);
        }

        private void InitializeDatabase()
        {
            using var connection = GetConnection();
            connection.Open();

            // Cria tabela
            ExecuteCommand(Sql_CreateBetRecord, connection);

            // 🔥 Cria índice único (evita duplicidade)
            ExecuteCommand(Sql_CreateUniqueIndex, connection);
        }

        public bool ExecuteCommand(string sql, SqliteConnection? connection = null)
        {
            try
            {
                bool shouldDispose = connection == null;

                if (connection == null)
                {
                    connection = GetConnection();
                    connection.Open();
                }

                using var command = connection.CreateCommand();
                command.CommandText = sql;

                command.ExecuteNonQuery();

                if (shouldDispose)
                    connection.Dispose();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro: {ex.Message}");
                return false;
            }
        }
    }
}
