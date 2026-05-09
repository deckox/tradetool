using Microsoft.Win32;
using Tradetool.Helpers;
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

                var inserted = _repository.Save(item);

                if (!inserted)
                {
                    Console.WriteLine($"❌ NÃO INSERIDO → BetId: {item.BetId}");
                }
                else
                {
                    Console.WriteLine($"✅ INSERIDO → BetId: {item.BetId}");
                }

                //ImportProgress.Percent = (int)((processados * 100.0) / total);
            }

            return inseridos;
        }
    }
}
