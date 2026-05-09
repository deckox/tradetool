using OfficeOpenXml;
using System.Drawing;
using System.Globalization;
using System.Runtime.Intrinsics.X86;
using Tradetool.Models;

namespace Tradetool.Helpers
{
    public class ExcelDataService
    {
        public List<BetRegister> LoadFromExcel(string path)
        {
            ExcelPackage.License.SetNonCommercialPersonal("Andre");

            using var package = new ExcelPackage(new FileInfo(path));
            if (package.Workbook.Worksheets.Count == 0)
                throw new Exception("Nenhuma planilha encontrada");

            var result = ReadAndMapExcel(package, path);

            return result;
        }

        public List<BetRecordData> ParseDataToBetRecordData(List<BetRegister> register)
        {
            var betRecordDataList = new List<BetRecordData>();

            foreach (var item in register)
            {
               var split = MapAndParseBetRecordData(item);
               if (split != null)
               {
                  betRecordDataList.Add(split);
               }
            }

            return betRecordDataList;
        }

        private List<BetRegister> ReadAndMapExcel(ExcelPackage excelPackage, string path)
        {
            // Mapear colunas pelo nome
            var headerMap = new Dictionary<string, int>();
            var ws = excelPackage.Workbook.Worksheets[0];

            var result = new List<BetRegister>();

            for (int col = 1; col <= ws.Dimension.Columns; col++)
            {
                var nome = ws.Cells[1, col].Text.Trim();
                headerMap[nome] = col;
            }

            // Percorrer linhas
            for (int row = 2; row <= ws.Dimension.Rows; row++)
            {
                var item = new BetRegister
                {
                    Date = ParseDate(ws, row, headerMap, "Date"),
                    Description = Get(ws, row, headerMap, "Description"),
                    System = Get(ws, row, headerMap, "System"),
                    BetType = Get(ws, row, headerMap, "Bet Type"),
                    BetId = ParseLong(ws, row, headerMap, "Bet Id"),
                    MarketId = ParseLong(ws, row, headerMap, "Market Id"),
                    Stake = ParseNullableDecimal(ws, row, headerMap, "Stake"),
                    Odds = ParseNullableDecimal(ws, row, headerMap, "Odds"),
                    Amount = ParseDecimal(ws, row, headerMap, "Amount"),
                    BalanceAfter = ParseDecimal(ws, row, headerMap, "Balance After")
                };

                result.Add(item);
            }

            return result;
        }

        private BetRecordData? MapAndParseBetRecordData(BetRegister item)
        {
            var aux = item.Description.Split('|').ToList();
            var auxCount = aux.Count;

            // ignora depósitos
            if (aux.FirstOrDefault()?.Contains("DEPOSIT", StringComparison.OrdinalIgnoreCase) == true 
                || auxCount > 4 && aux.LastOrDefault() == "BACK")
                return null;

            var team = aux[1].Split("vs").ToList();

            var betId = item.BetId > 0
                ? item.BetId
                : GenerateFallbackId(item); // 🔥 aqui está o segredo

            var betRecordData = new BetRecordData
            {
                BetId = betId,
                Date = item.Date,
                Stake = item.Stake.HasValue ? Math.Round(item.Stake.Value, 2, MidpointRounding.AwayFromZero) : null,
                Responsability = aux[3].Contains("COMMISSION") ? 0 :
                    (item.Odds.HasValue && item.Stake.HasValue
                        ? Math.Round((item.Odds.Value - 1) * item.Stake.Value, 0, MidpointRounding.AwayFromZero)
                        : 0),
                PL = CalculateProfitLoss(item, aux),
                PLStake = CalculatePLStake(item),
                Odds = item.Odds,
                Amount = Math.Round(item.Amount, 2, MidpointRounding.AwayFromZero),
                BalanceAfter = Math.Round(item.BalanceAfter, 2, MidpointRounding.AwayFromZero),
                Home = team.FirstOrDefault()?.Trim(),
                Away = team.LastOrDefault()?.Trim(),
                Competition = aux.FirstOrDefault()?.Trim() ?? string.Empty,
                Market = aux[2]?.Trim() ?? string.Empty,
                Methods = aux[3].Contains("COMMISSION") ? string.Empty : aux[3].Trim(),
                Side = aux.Count > 4 ? aux.LastOrDefault()?.Trim() : string.Empty
            };

            return betRecordData;
        }

        private decimal CalculateProfitLoss(BetRegister item, List<string> aux)
        {
            if(aux.Count() > 4 && aux[4] == "BACK")
            {

            }
            return item.Stake.HasValue ? Math.Round(item.Stake.Value, 2, MidpointRounding.AwayFromZero) : 0;
        }


        private decimal CalculatePLStake(BetRegister item)
        {
            decimal result = 0;
            var aux = item.Description.Split('|').ToList();

            var responsability = aux[3].Contains("COMMISSION") ? 0 : (item.Odds.HasValue && item.Stake.HasValue
                           ? Math.Round((item.Odds.Value - 1) * item.Stake.Value, 0, MidpointRounding.AwayFromZero) : 0);
            var pl = item.Stake.HasValue ? Math.Round(item.Stake.Value, 2, MidpointRounding.AwayFromZero) : 0;

            if (responsability != 0)
            {
                result = pl / responsability;
            }
           
            return Math.Round(result, 2, MidpointRounding.AwayFromZero);
        }

        private string Get(ExcelWorksheet ws, int row, Dictionary<string, int> map, string col)
        {
            return ws.Cells[row, map[col]].Text;
        }

        private decimal ParseDecimal(ExcelWorksheet ws, int row, Dictionary<string, int> map, string col)
        {
            var text = ws.Cells[row, map[col]].Text;

            return decimal.TryParse(text, NumberStyles.Any, new CultureInfo("pt-BR"), out var v)
                ? v
                : 0;
        }

        private decimal? ParseNullableDecimal(ExcelWorksheet ws, int row, Dictionary<string, int> map, string col)
        {
            var text = ws.Cells[row, map[col]].Text;

            if (string.IsNullOrWhiteSpace(text))
                return null;

            return decimal.TryParse(text, NumberStyles.Any, new CultureInfo("pt-BR"), out var v)
                ? v
                : null;
        }

        private long ParseLong(ExcelWorksheet ws, int row, Dictionary<string, int> map, string col)
        {
            var text = ws.Cells[row, map[col]].Text;

            return long.TryParse(text, out var v) ? v : 0;
        }

        private DateTime ParseDate(ExcelWorksheet ws, int row, Dictionary<string, int> map, string col)
        {
            //var text = ws.Cells[row, map[col]].Text;

            return ws.Cells[row, map[col]].GetValue<DateTime>();

            //return DateTime.ParseExact(text,"d/M/yy H:mm",new CultureInfo("pt-BR"));
        }

        private int GenerateFallbackId(BetRegister item)
        {
            return HashCode.Combine(
                item.Date,
                item.Amount,
                item.Description
            );
        }
    }
}
