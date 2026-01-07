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
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public List<Prijava> PrijaveZaObcino { get; set; } = new List<Prijava>();

        public ObcinaModel()
        {
            _jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "users.json");
        }

        public async Task<IActionResult> OnGetAsync()
        {

            if (!User.IsInRole("Admin"))
                return RedirectToPage("/Login");

            var allUsers = await PreberiUporabnikeAsync();
            if (allUsers == null)
            {
                PrijaveZaObcino = new List<Prijava>();
                return Page();
            }

            var currentUsername = User.Identity?.Name;
            var currentAdmin = allUsers.FirstOrDefault(u => u.Username.Equals(currentUsername, StringComparison.OrdinalIgnoreCase));
            var adminObcina = currentAdmin?.Obcina;

            var vsePrijave = allUsers
                .Where(u => u?.Prijave != null)
                .SelectMany(u => u.Prijave!)
                .ToList();

            if (!string.IsNullOrEmpty(adminObcina))
            {
                vsePrijave = vsePrijave
                    .Where(p => p.Obcina != null && p.Obcina.Equals(adminObcina, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            PrijaveZaObcino = vsePrijave
                .OrderByDescending(p => (int)p.Nujnost)
                .ThenBy(p => p.JeReseno)
                .ThenByDescending(p => p.SteviloVseckov)
                .ThenByDescending(p => p.Datum)
                .ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostNastaviPrioritetoAsync(Guid id, int prioriteta)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToPage("/Login");

            bool ok = await NastaviPrioritetoAsync(id, (NivoPrioritet)prioriteta);
            if (ok)
            {
                TempData["Message"] = "Prioriteta je bila uspešno nastavljena.";
            }
            else
            {
                TempData["Error"] = "Napaka pri nastavljanju prioritete.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostOznaciKotResenoAsync(Guid id)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToPage("/Login");

            bool ok = await NastaviStatusAsync(id, true);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostOznaciKotVObdelaviAsync(Guid id)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToPage("/Login");

            bool ok = await NastaviStatusAsync(id, false);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostArhivirajAsync(Guid id)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToPage("/Login");

            bool ok = await NastaviArhivAsync(id, true);
            if (ok)
            {
                TempData["Message"] = "Prijava je bila uspešno arhivirana.";
            }
            else
            {
                TempData["Error"] = "Napaka pri arhiviranju prijave.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRazveljavljArhivAsync(Guid id)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToPage("/Login");

            bool ok = await NastaviArhivAsync(id, false);
            if (ok)
            {
                TempData["Message"] = "Arhiviranje je bilo razveljavljeno.";
            }
            else
            {
                TempData["Error"] = "Napaka pri razveljavitvi arhiviranja.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            if (!User.IsInRole("Admin"))
                return RedirectToPage("/Login");

            bool ok = await DeletePrijavaAsync(id);
            if (ok)
            {
                TempData["Message"] = "Prijava je bila uspešno izbrisana.";
            }
            else
            {
                TempData["Error"] = "Napaka pri brisanju prijave. Prijava ne obstaja ali ne spada v vašo ob?ino.";
            }
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

        private async Task<bool> NastaviArhivAsync(Guid prijavaId, bool jeArhivirano)
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
                    Prijava? target = null;
                    foreach (var u in allUsers)
                    {
                        if (u?.Prijave == null) continue;

                        target = u.Prijave.FirstOrDefault(p => p.Id == prijavaId);
                        if (target != null) break;
                    }

                    if (target == null) return false;

                    target.JeArhivirano = jeArhivirano;
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

        private async Task<bool> NastaviPrioritetoAsync(Guid prijavaId, NivoPrioritet prioriteta)
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

                    Prijava? target = null;
                    foreach (var u in allUsers)
                    {
                        if (u?.Prijave == null) continue;

                        target = u.Prijave.FirstOrDefault(p => p.Id == prijavaId);
                        if (target != null) break;
                    }

                    if (target == null) return false;

                    target.Nujnost = prioriteta;
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

        private async Task<bool> DeletePrijavaAsync(Guid prijavaId)
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

                    // Najdi trenutnega admina in njegovo ob?ino
                    var currentUsername = User.Identity?.Name;
                    var currentAdmin = allUsers.FirstOrDefault(u => u.Username.Equals(currentUsername, StringComparison.OrdinalIgnoreCase));
                    var adminObcina = currentAdmin?.Obcina;

                    // Najdi prijavo in preveri, ali spada v ob?ino admina
                    Prijava? target = null;
                    UserCredentials? targetUser = null;
                    
                    foreach (var u in allUsers)
                    {
                        if (u?.Prijave == null) continue;

                        target = u.Prijave.FirstOrDefault(p => p.Id == prijavaId);
                        if (target != null)
                        {
                            targetUser = u;
                            break;
                        }
                    }

                    if (target == null || targetUser == null) return false;

                    // Preveri, ali prijava spada v ob?ino admina
                    if (!string.IsNullOrEmpty(adminObcina) && 
                        (target.Obcina == null || !target.Obcina.Equals(adminObcina, StringComparison.OrdinalIgnoreCase)))
                    {
                        return false; // Prijava ne spada v ob?ino admina
                    }

                    // Odstrani prijavo
                    targetUser.Prijave.Remove(target);

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
    }
}
