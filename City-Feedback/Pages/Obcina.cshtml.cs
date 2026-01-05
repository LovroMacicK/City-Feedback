using City_Feedback.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace City_Feedback.Pages
{
    public class ObcinaModel : PageModel
    {
        private readonly string _jsonFilePath;
        private const int MaxRetries = 3;

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public List<Prijava> PrijaveZaObcino { get; set; } = new List<Prijava>();

        public ObcinaModel()
        {
            _jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "users.json");
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Samo obèina/admin
            if (!User.IsInRole("Admin"))
                return RedirectToPage("/Login");

            var allUsers = await PreberiUporabnikeAsync();
            if (allUsers == null)
            {
                PrijaveZaObcino = new List<Prijava>();
                return Page();
            }

            // Vse prijave vseh uporabnikov
            var vsePrijave = allUsers
                .Where(u => u?.Prijave != null)
                .SelectMany(u => u.Prijave!)
                .ToList();

            // Sort: nerešene (v obdelavi) najprej, potem po všeèkih, potem po datumu
            PrijaveZaObcino = vsePrijave
                .OrderBy(p => p.JeReseno)
                .ThenByDescending(p => p.SteviloVseckov)
                .ThenByDescending(p => p.Datum)
                .ToList();

            return Page();
        }

        // Handler: nastavi status na opravljeno (JeReseno = true)
        public async Task<IActionResult> OnPostOznaciKotResenoAsync(Guid id)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToPage("/Login");

            bool ok = await NastaviStatusAsync(id, true);
            return RedirectToPage();
        }

        // Handler: nastavi status nazaj na v obdelavi (JeReseno = false)
        public async Task<IActionResult> OnPostOznaciKotVObdelaviAsync(Guid id)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToPage("/Login");

            bool ok = await NastaviStatusAsync(id, false);
            return RedirectToPage();
        }

        private async Task<bool> NastaviStatusAsync(Guid prijavaId, bool jeReseno)
        {
            if (!System.IO.File.Exists(_jsonFilePath))
                return false;

            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                try
                {
                    string json = await System.IO.File.ReadAllTextAsync(_jsonFilePath);
                    var allUsers = JsonSerializer.Deserialize<List<UserCredentials>>(json, _jsonOptions);
                    if (allUsers == null) return false;

                    // Najdi prijavo v kateremkoli uporabniku
                    Prijava? target = null;
                    foreach (var u in allUsers)
                    {
                        if (u?.Prijave == null) continue;

                        target = u.Prijave.FirstOrDefault(p => p.Id == prijavaId);
                        if (target != null) break;
                    }

                    if (target == null) return false;

                    target.JeReseno = jeReseno;
                    target.LastUpdated = DateTime.Now;

                    string updated = JsonSerializer.Serialize(allUsers, _jsonOptions);
                    await System.IO.File.WriteAllTextAsync(_jsonFilePath, updated);

                    return true;
                }
                catch (IOException) when (attempt < MaxRetries - 1)
                {
                    await Task.Delay(200);
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        private async Task<List<UserCredentials>?> PreberiUporabnikeAsync()
        {
            if (!System.IO.File.Exists(_jsonFilePath))
                return null;

            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                try
                {
                    string json = await System.IO.File.ReadAllTextAsync(_jsonFilePath);
                    return JsonSerializer.Deserialize<List<UserCredentials>>(json, _jsonOptions);
                }
                catch (IOException) when (attempt < MaxRetries - 1)
                {
                    await Task.Delay(200);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }
    }
}
