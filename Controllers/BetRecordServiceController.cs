using Microsoft.AspNetCore.Mvc;
using Tradetool.Core;
using Tradetool.Models;

namespace Tradetool.Controllers
{
    public class BetController : Controller
    {
        private readonly BetRecord _betRecord;
        private readonly BetImportService _importService;

        public BetController(BetRecord betRecord)
        {
            _betRecord = betRecord;
            _importService = new BetImportService();
        }

        // =========================
        // IMPORT
        // =========================

        [HttpGet]
        public IActionResult Import()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ViewBag.Error = "Selecione um arquivo.";

                return View();
            }

            var uploadsFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var filePath = Path.Combine(
                uploadsFolder,
                file.FileName);

            using (var stream = new FileStream(
                filePath,
                FileMode.Create))
            {
                file.CopyTo(stream);
            }

            var total =
                _importService.ImportBetRecords(filePath);

            ViewBag.Message =
                $"{total} registros importados com sucesso.";

            return View();
        }

        // =========================
        // DASHBOARD PRINCIPAL
        // =========================
        public IActionResult Dashboard(string? team, string? month)
        {
            var data = _betRecord.GetDashboard(team, month);
            return View(data);
        }

        // =========================
        // ANÁLISE POR TIME
        // =========================
        public IActionResult TeamAnalysis(string? team, string? month)
        {
            var data = _betRecord.GetDashboard(team, month);

            ViewBag.SelectedMonth = month;

            return View(data);
        }

        // =========================
        // AUTOCOMPLETE TIMES
        // =========================
        [HttpGet]
        public IActionResult GetTeams()
        {
            var teams = _betRecord.GetAllTeams();
            return Json(teams);
        }

        // =========================
        // GRÁFICO POR TIME
        // =========================
        [HttpGet]
        public IActionResult GetTeamChart(string team)
        {
            if (string.IsNullOrEmpty(team))
                return Json(new List<SaldoEvolucao>());

            var data = _betRecord.GetEvolucaoPorTime(team);
            return Json(data);
        }

        // =========================
        // HISTORY
        // =========================
        public IActionResult History(
            string? period = null,
            string? startDate = null,
            string? endDate = null,
            string? month = null)
        {
            var history = _betRecord.GetHistory(
                period,
                startDate,
                endDate,
                month);

            ViewBag.Period = period;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.Month = month;

            return View(history);
        }
    }
}