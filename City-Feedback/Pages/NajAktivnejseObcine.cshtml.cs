using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using City_Feedback.Models;

namespace City_Feedback.Pages
{
    public class NajAktivnejseObcineModel : PageModel
    {
        private readonly string _jsonFilePath;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public NajAktivnejseObcineModel()
        {
            _jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "users.json");
        }

        public List<ObcinaStatistika> VseObcine { get; set; } = new List<ObcinaStatistika>();
        public List<ObcinaStatistika> Top5Obcine { get; set; } = new List<ObcinaStatistika>();

        [BindProperty(SupportsGet = true)]
        public string SearchQuery { get; set; }

        public IActionResult OnGet()
        {
            LoadStatistics();
            return Page();
        }

        private void LoadStatistics()
        {
            if (!System.IO.File.Exists(_jsonFilePath))
            {
                return;
            }

            try
            {
                string jsonString = System.IO.File.ReadAllText(_jsonFilePath);
                var allUsers = JsonSerializer.Deserialize<List<UserCredentials>>(jsonString, _jsonOptions);

                if (allUsers == null)
                {
                    return;
                }

                // Zberi vse admin račune z občinami
                var adminUsers = allUsers
                    .Where(u => u.IsAdmin && !string.IsNullOrEmpty(u.Obcina))
                    .ToList();

                // Zberi vse prijave vseh uporabnikov
                var vsePrijave = allUsers
                    .Where(u => u.Prijave != null)
                    .SelectMany(u => u.Prijave)
                    .ToList();

                // Ustvari statistiko za vsako občino
                var obcineStatistike = adminUsers
                    .GroupBy(u => u.Obcina)
                    .Select(g =>
                    {
                        var obcinaPrijave = vsePrijave
                            .Where(p => p.Obcina != null && p.Obcina.Equals(g.Key, StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        return new ObcinaStatistika
                        {
                            ImeObcine = g.Key,
                            SteviloResenih = obcinaPrijave.Count(p => p.JeReseno),
                            SteviloVObdelavi = obcinaPrijave.Count(p => !p.JeReseno),
                            SkupnoPrijav = obcinaPrijave.Count,
                            SkupnoUpvotov = obcinaPrijave.Sum(p => p.Upvotes),
                            SkupnoDownvotov = obcinaPrijave.Sum(p => p.Downvotes),
                            ProcentResenih = obcinaPrijave.Count > 0 
                                ? Math.Round((double)obcinaPrijave.Count(p => p.JeReseno) / obcinaPrijave.Count * 100, 1) 
                                : 0
                        };
                    })
                    .OrderByDescending(o => o.SteviloResenih)
                    .ThenByDescending(o => o.SkupnoPrijav)
                    .ToList();

                VseObcine = obcineStatistike;
                Top5Obcine = obcineStatistike.Take(5).ToList();

                // Filtriraj če je iskalnik aktiven
                if (!string.IsNullOrEmpty(SearchQuery))
                {
                    VseObcine = VseObcine
                        .Where(o => o.ImeObcine.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
            }
            catch
            {
                // V primeru napake vrni prazne sezname
            }
        }
    }

    public class ObcinaStatistika
    {
        public string ImeObcine { get; set; }
        public int SteviloResenih { get; set; }
        public int SteviloVObdelavi { get; set; }
        public int SkupnoPrijav { get; set; }
        public int SkupnoUpvotov { get; set; }
        public int SkupnoDownvotov { get; set; }
        public double ProcentResenih { get; set; }
    }
}
