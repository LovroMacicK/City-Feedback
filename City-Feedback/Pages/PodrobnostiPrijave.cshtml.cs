using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Text.Json;
using City_Feedback.Models;

namespace City_Feedback.Pages
{
    public class PodrobnostiPrijaveModel : PageModel
    {
        private readonly string _jsonFilePath;
        private const int MaxRetries = 3;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public PodrobnostiPrijaveModel()
        {
            _jsonFilePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "users.json");
        }

        public Prijava Prijava { get; set; }
        public string CurrentUsername { get; set; }
        public bool IsAuthenticated { get; set; }

        [BindProperty]
        public string KomentarVsebina { get; set; }

        public IActionResult OnGet(Guid id)
        {
            CurrentUsername = User.FindFirst(ClaimTypes.Name)?.Value;
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false;

            if (!LoadPrijava(id))
            {
                TempData["Error"] = "Prijava ne obstaja.";
                return RedirectToPage("/Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAddCommentAsync(Guid id)
        {
            CurrentUsername = User.FindFirst(ClaimTypes.Name)?.Value;
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false;

            if (!IsAuthenticated)
            {
                TempData["Error"] = "Za dodajanje komentarja se morate prijaviti.";
                return RedirectToPage("/Login");
            }

            if (string.IsNullOrWhiteSpace(KomentarVsebina))
            {
                ModelState.AddModelError(nameof(KomentarVsebina), "Komentar ne sme biti prazen.");
                LoadPrijava(id);
                return Page();
            }

            bool success = await AddKomentar(id, CurrentUsername, KomentarVsebina);

            if (success)
            {
                TempData["Message"] = "Komentar je bil uspešno dodan!";
            }
            else
            {
                TempData["Error"] = "Napaka pri dodajanju komentarja.";
            }

            return RedirectToPage(new { id = id });
        }

        public async Task<IActionResult> OnPostUpvoteAsync(Guid id)
        {
            CurrentUsername = User.FindFirst(ClaimTypes.Name)?.Value;
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false;

            if (!IsAuthenticated)
            {
                TempData["Error"] = "Za glasovanje se morate prijaviti.";
                return RedirectToPage("/Login");
            }

            await ToggleUpvote(id, CurrentUsername);

            return RedirectToPage(new { id = id });
        }

        public async Task<IActionResult> OnPostDownvoteAsync(Guid id)
        {
            CurrentUsername = User.FindFirst(ClaimTypes.Name)?.Value;
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false;

            if (!IsAuthenticated)
            {
                TempData["Error"] = "Za glasovanje se morate prijaviti.";
                return RedirectToPage("/Login");
            }

            await ToggleDownvote(id, CurrentUsername);

            return RedirectToPage(new { id = id });
        }

        private bool LoadPrijava(Guid prijavaId)
        {
            if (!System.IO.File.Exists(_jsonFilePath))
            {
                return false;
            }

            try
            {
                string jsonString = System.IO.File.ReadAllText(_jsonFilePath);
                var allUsers = JsonSerializer.Deserialize<List<UserCredentials>>(jsonString, _jsonOptions);

                if (allUsers == null)
                {
                    return false;
                }

                foreach (var user in allUsers)
                {
                    if (user.Prijave != null)
                    {
                        var prijava = user.Prijave.FirstOrDefault(p => p.Id == prijavaId);
                        if (prijava != null)
                        {
                            Prijava = prijava;
                            return true;
                        }
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> AddKomentar(Guid prijavaId, string username, string vsebina)
        {
            if (!System.IO.File.Exists(_jsonFilePath))
            {
                return false;
            }

            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                try
                {
                    string jsonString = await System.IO.File.ReadAllTextAsync(_jsonFilePath);
                    var allUsers = JsonSerializer.Deserialize<List<UserCredentials>>(jsonString, _jsonOptions);

                    if (allUsers == null)
                    {
                        return false;
                    }

                    var commentUser = allUsers.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

                    if (commentUser == null)
                    {
                        return false;
                    }

                    Prijava targetPrijava = null;
                    foreach (var user in allUsers)
                    {
                        if (user.Prijave != null)
                        {
                            targetPrijava = user.Prijave.FirstOrDefault(p => p.Id == prijavaId);
                            if (targetPrijava != null)
                            {
                                break;
                            }
                        }
                    }

                    if (targetPrijava == null)
                    {
                        return false;
                    }

                    if (targetPrijava.Komentarji == null)
                    {
                        targetPrijava.Komentarji = new List<Komentar>();
                    }

                    var novKomentar = new Komentar
                    {
                        Id = Guid.NewGuid(),
                        Vsebina = vsebina,
                        Datum = DateTime.Now,
                        Username = commentUser.Username,
                        UserFullName = !string.IsNullOrEmpty(commentUser.FullName) ? commentUser.FullName : commentUser.Username,
                        UserProfilePicture = !string.IsNullOrEmpty(commentUser.ProfilePicturePath)
                            ? commentUser.ProfilePicturePath
                            : "/images/default_profile.png"
                    };

                    targetPrijava.Komentarji.Add(novKomentar);

                    var updatedJsonString = JsonSerializer.Serialize(allUsers, _jsonOptions);
                    await System.IO.File.WriteAllTextAsync(_jsonFilePath, updatedJsonString);

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

        private async Task<string> ToggleUpvote(Guid prijavaId, string username)
        {
            if (!System.IO.File.Exists(_jsonFilePath))
            {
                return "error";
            }

            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                try
                {
                    string jsonString = await System.IO.File.ReadAllTextAsync(_jsonFilePath);
                    var allUsers = JsonSerializer.Deserialize<List<UserCredentials>>(jsonString, _jsonOptions);

                    if (allUsers == null)
                    {
                        return "error";
                    }

                    Prijava targetPrijava = null;

                    foreach (var user in allUsers)
                    {
                        if (user.Prijave != null)
                        {
                            targetPrijava = user.Prijave.FirstOrDefault(p => p.Id == prijavaId);
                            if (targetPrijava != null)
                            {
                                break;
                            }
                        }
                    }

                    if (targetPrijava == null)
                    {
                        return "error";
                    }

                    if (targetPrijava.UpvotedBy == null)
                    {
                        targetPrijava.UpvotedBy = new List<string>();
                    }
                    if (targetPrijava.DownvotedBy == null)
                    {
                        targetPrijava.DownvotedBy = new List<string>();
                    }

                    string result;

                    // Remove downvote if exists
                    if (targetPrijava.DownvotedBy.Contains(username))
                    {
                        targetPrijava.DownvotedBy.Remove(username);
                        targetPrijava.Downvotes = Math.Max(0, targetPrijava.Downvotes - 1);
                    }

                    // Toggle upvote
                    if (targetPrijava.UpvotedBy.Contains(username))
                    {
                        targetPrijava.UpvotedBy.Remove(username);
                        targetPrijava.Upvotes = Math.Max(0, targetPrijava.Upvotes - 1);
                        result = "removed";
                    }
                    else
                    {
                        targetPrijava.UpvotedBy.Add(username);
                        targetPrijava.Upvotes++;
                        result = "added";
                    }

                    var updatedJsonString = JsonSerializer.Serialize(allUsers, _jsonOptions);
                    await System.IO.File.WriteAllTextAsync(_jsonFilePath, updatedJsonString);

                    return result;
                }
                catch (IOException) when (attempt < MaxRetries - 1)
                {
                    await Task.Delay(200);
                }
                catch
                {
                    return "error";
                }
            }

            return "error";
        }

        private async Task<string> ToggleDownvote(Guid prijavaId, string username)
        {
            if (!System.IO.File.Exists(_jsonFilePath))
            {
                return "error";
            }

            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                try
                {
                    string jsonString = await System.IO.File.ReadAllTextAsync(_jsonFilePath);
                    var allUsers = JsonSerializer.Deserialize<List<UserCredentials>>(jsonString, _jsonOptions);

                    if (allUsers == null)
                    {
                        return "error";
                    }

                    Prijava targetPrijava = null;

                    foreach (var user in allUsers)
                    {
                        if (user.Prijave != null)
                        {
                            targetPrijava = user.Prijave.FirstOrDefault(p => p.Id == prijavaId);
                            if (targetPrijava != null)
                            {
                                break;
                            }
                        }
                    }

                    if (targetPrijava == null)
                    {
                        return "error";
                    }

                    if (targetPrijava.UpvotedBy == null)
                    {
                        targetPrijava.UpvotedBy = new List<string>();
                    }
                    if (targetPrijava.DownvotedBy == null)
                    {
                        targetPrijava.DownvotedBy = new List<string>();
                    }

                    string result;

                    // Remove upvote if exists
                    if (targetPrijava.UpvotedBy.Contains(username))
                    {
                        targetPrijava.UpvotedBy.Remove(username);
                        targetPrijava.Upvotes = Math.Max(0, targetPrijava.Upvotes - 1);
                    }

                    // Toggle downvote
                    if (targetPrijava.DownvotedBy.Contains(username))
                    {
                        targetPrijava.DownvotedBy.Remove(username);
                        targetPrijava.Downvotes = Math.Max(0, targetPrijava.Downvotes - 1);
                        result = "removed";
                    }
                    else
                    {
                        targetPrijava.DownvotedBy.Add(username);
                        targetPrijava.Downvotes++;
                        result = "added";
                    }

                    var updatedJsonString = JsonSerializer.Serialize(allUsers, _jsonOptions);
                    await System.IO.File.WriteAllTextAsync(_jsonFilePath, updatedJsonString);

                    return result;
                }
                catch (IOException) when (attempt < MaxRetries - 1)
                {
                    await Task.Delay(200);
                }
                catch
                {
                    return "error";
                }
            }

            return "error";
        }
    }
}