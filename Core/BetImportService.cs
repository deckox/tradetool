using Microsoft.Win32;
using Tradetool.Helpers;
using Tradetool.Models;
using static Tradetool.Controllers.BetController;

namespace Tradetool.Core
{
    public class BetImportService
    {
        private readonly ExcelDataService _excelService;
        private readonly BetRecord _repository;

        public BetImportService()
        {
            _excelService = new ExcelDataService();
            _repository = new BetRecord();
        }

        public int ImportBetRecords(string path)
        {
            var registers = _excelService.LoadFromExcel(path);
            var registrosConvertidos = _excelService.ParseDataToBetRecordData(registers);

            int total = registrosConvertidos.Count;
            int inseridos = 0;
            int processados = 0;

            foreach (var item in registrosConvertidos)
            {
                processados++;

                if (item == null)
                    continue;

                // =====================================
                // TARGET TEAM
                // =====================================

                // =====================================
                // TARGET TEAM
                // =====================================

                item.TargetTeam = DetectTargetTeam(item);

                Console.WriteLine(
                    $"SIDE={item.Side} | " +
                    $"SELECTION={item.Selection} | " +
                    $"HOME={item.Home} | " +
                    $"AWAY={item.Away} | " +
                    $"TARGET={item.TargetTeam}");

                var inserted = _repository.Save(item);

                if (!inserted)
                {
                    Console.WriteLine($"❌ NÃO INSERIDO → BetId: {item.BetId}");
                }
                else
                {
                    Console.WriteLine($"✅ INSERIDO → BetId: {item.BetId}");
                }
            }

            return inseridos;
        }

        private string DetectTargetTeam(BetRecordData item)
        {
            if (string.IsNullOrWhiteSpace(item.Selection))
                return "";

            var selection =
                item.Selection
                    .Trim()
                    .ToUpper();

            // =========================
            // ANY OTHER HOME WIN
            // =========================

            if (selection.Contains("ANY OTHER HOME WIN"))
            {
                return item.Home ?? "";
            }

            // =========================
            // ANY OTHER AWAY WIN
            // =========================

            if (selection.Contains("ANY OTHER AWAY WIN"))
            {
                return item.Away ?? "";
            }

            // =========================
            // SCORE X-Y
            // =========================

            var parts = selection.Split('-');

            if (parts.Length != 2)
                return "";

            if (!int.TryParse(parts[0], out int homeGoals))
                return "";

            if (!int.TryParse(parts[1], out int awayGoals))
                return "";

            // vitória mandante

            if (homeGoals > awayGoals)
            {
                return item.Home ?? "";
            }

            // vitória visitante

            if (awayGoals > homeGoals)
            {
                return item.Away ?? "";
            }

            // empate

            return "";
        }
    }
}
