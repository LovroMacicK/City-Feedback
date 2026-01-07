using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Security.Claims;
using City_Feedback.Models;

namespace City_Feedback.Pages
{
    public class PrijaveModel : PageModel
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly string _jsonFilePath;
        private const int MaxRetries = 3;
        private const long MaxFileSize = 5 * 1024 * 1024;

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public PrijaveModel(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
            _jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "users.json");
        }

        [BindProperty]
        public string Title { get; set; }

        [BindProperty]
        public string Description { get; set; }

        [BindProperty]
        public IFormFile Image { get; set; }

        [BindProperty]
        public string Category { get; set; }

        [BindProperty]
        public string Obcina { get; set; }

        [BindProperty]
        public string Lokacija { get; set; }

        public List<Prijava> Prijave { get; set; } = new List<Prijava>();
        public string CurrentUsername { get; set; }

        public void OnGet(string sortOrder, string status, string category, string obcina, string searchTerm)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["CurrentStatus"] = status ?? "all";
            ViewData["CurrentCategory"] = category ?? "all";
            ViewData["CurrentObcina"] = obcina ?? "all";
            ViewData["CurrentSearch"] = searchTerm;
            CurrentUsername = User.FindFirst(ClaimTypes.Name)?.Value;
            LoadAllPrijave(sortOrder, status, category, obcina, searchTerm);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var currentUsername = User.FindFirst(ClaimTypes.Name)?.Value;

                if (string.IsNullOrEmpty(currentUsername))
                {
                    TempData["Error"] = "Za dodajanje prijave se morate prijaviti.";
                    return RedirectToPage("/Login");
                }

                ModelState.Remove(nameof(Image));

                if (string.IsNullOrWhiteSpace(Title))
                {
                    ModelState.AddModelError(nameof(Title), "Naslov je obvezen.");
                }

                if (string.IsNullOrWhiteSpace(Description))
                {
                    ModelState.AddModelError(nameof(Description), "Opis je obvezen.");
                }

                if (string.IsNullOrWhiteSpace(Lokacija))
                {
                    ModelState.AddModelError(nameof(Lokacija), "Lokacija je obvezna.");
                }

                if (Image != null && Image.Length > 0)
                {
                    if (Image.Length > MaxFileSize)
                    {
                        ModelState.AddModelError(nameof(Image), $"Slika ne sme biti večja od {MaxFileSize / 1024 / 1024} MB.");
                    }

                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var extension = Path.GetExtension(Image.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError(nameof(Image), "Dovoljene so samo slike (.jpg, .jpeg, .png, .gif).");
                    }
                }

                if (!ModelState.IsValid)
                {
                    CurrentUsername = currentUsername;
                    LoadAllPrijave(null, null, null, null, null);
                    return Page();
                }

                string imagePath = await ProcessUploadedImage();

                string ownerProfilePicture = "/images/default_profile.png";
                string ownerFullName = currentUsername;

                var jsonString = await System.IO.File.ReadAllTextAsync(_jsonFilePath);
                var allUsers = JsonSerializer.Deserialize<List<UserCredentials>>(jsonString, _jsonOptions);
                var currentUser = allUsers?.FirstOrDefault(u => u.Username.Equals(currentUsername, StringComparison.OrdinalIgnoreCase));

                if (currentUser != null)
                {
                    ownerProfilePicture = !string.IsNullOrEmpty(currentUser.ProfilePicturePath)
                        ? currentUser.ProfilePicturePath
                        : "/images/default_profile.png";
                    ownerFullName = !string.IsNullOrEmpty(currentUser.FullName)
                        ? currentUser.FullName
                        : currentUsername;
                }

                var novaPrijava = new Prijava
                {
                    Id = Guid.NewGuid(),
                    Naslov = Title,
                    Opis = Description,
                    Datum = DateTime.Now,
                    SlikaPot = imagePath,
                    SteviloVseckov = 0,
                    JeReseno = false,
                    LikedBy = new List<string>(),
                    OwnerUsername = ownerFullName,
                    OwnerProfilePicture = ownerProfilePicture,
                    Kategorija = Category ?? "Ostalo",
                    Obcina = Obcina ?? "Ljubljana",
                    Lokacija = Lokacija
                };

                bool success = await AddPrijavaToUser(currentUsername, novaPrijava);

                if (success)
                {
                    TempData["Message"] = "Prijava je bila uspešno objavljena!";
                    return RedirectToPage("/Index");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Shranjevanje ni uspelo. Poskusite ponovno.");
                    CurrentUsername = currentUsername;
                    LoadAllPrijave(null, null, null, null, null);
                    return Page();
                }
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "Prišlo je do napake pri shranjevanju.");
                CurrentUsername = User.FindFirst(ClaimTypes.Name)?.Value;
                LoadAllPrijave(null, null, null, null, null);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostUpvoteAsync(Guid id)
        {
            var currentUsername = User.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(currentUsername))
            {
                TempData["Error"] = "Za glasovanje se morate prijaviti.";
                return RedirectToPage("/Login");
            }

            await ToggleUpvote(id, currentUsername);

            return RedirectToPage("/Index");
        }

        public async Task<IActionResult> OnPostDownvoteAsync(Guid id)
        {
            var currentUsername = User.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(currentUsername))
            {
                TempData["Error"] = "Za glasovanje se morate prijaviti.";
                return RedirectToPage("/Login");
            }

            await ToggleDownvote(id, currentUsername);

            return RedirectToPage("/Index");
        }

        private void LoadAllPrijave(string sortOrder, string status, string category, string obcina, string searchTerm)
        {
            if (!System.IO.File.Exists(_jsonFilePath))
            {
                Prijave = new List<Prijava>();
                return;
            }

            try
            {
                string jsonString = System.IO.File.ReadAllText(_jsonFilePath);
                var allUsers = JsonSerializer.Deserialize<List<UserCredentials>>(jsonString, _jsonOptions);

                var allPrijave = new List<Prijava>();

                if (allUsers != null)
                {
                    foreach (var user in allUsers)
                    {
                        if (user.Prijave != null)
                        {
                            allPrijave.AddRange(user.Prijave);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    allPrijave = allPrijave.Where(p => 
                        p.Naslov != null && 
                        p.Naslov.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                if (status != null && status != "all")
                {
                    allPrijave = allPrijave.Where(p => p.JeReseno == (status == "resolved")).ToList();
                }

                if (!string.IsNullOrEmpty(category) && category != "all")
                {
                    allPrijave = allPrijave.Where(p => p.Kategorija == category).ToList();
                }

                if (!string.IsNullOrEmpty(obcina) && obcina != "all")
                {
                    allPrijave = allPrijave.Where(p => p.Obcina == obcina).ToList();
                }

                allPrijave = allPrijave.Where(p => !p.JeArhivirano).ToList();

                if (sortOrder == "likes")
                {
                    allPrijave = allPrijave.OrderByDescending(p => p.Upvotes - p.Downvotes).ToList();
                }
                else
                {
                    allPrijave = allPrijave.OrderByDescending(p => p.Datum).ToList();
                }

                Prijave = allPrijave;
            }
            catch
            {
                Prijave = new List<Prijava>();
            }
        }

        private async Task<bool> AddPrijavaToUser(string username, Prijava novaPrijava)
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

                    var userToUpdate = allUsers.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

                    if (userToUpdate == null)
                    {
                        return false;
                    }

                    if (userToUpdate.Prijave == null)
                    {
                        userToUpdate.Prijave = new List<Prijava>();
                    }

                    userToUpdate.Prijave.Add(novaPrijava);

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

                    if (targetPrijava.DownvotedBy.Contains(username))
                    {
                        targetPrijava.DownvotedBy.Remove(username);
                        targetPrijava.Downvotes = Math.Max(0, targetPrijava.Downvotes - 1);
                    }

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

                    if (targetPrijava.UpvotedBy.Contains(username))
                    {
                        targetPrijava.UpvotedBy.Remove(username);
                        targetPrijava.Upvotes = Math.Max(0, targetPrijava.Upvotes - 1);
                    }

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

        private async Task<string> ProcessUploadedImage()
        {
            if (Image == null || Image.Length == 0)
            {
                return null;
            }

            try
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "prijave");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(Image.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await Image.CopyToAsync(fileStream);
                }

                return $"/images/prijave/{uniqueFileName}";
            }
            catch
            {
                return null;
            }
        }
    }
}