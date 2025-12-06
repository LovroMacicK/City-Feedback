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
    public class ProfileModel : PageModel
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly string _jsonFilePath;
        private const string JsonFileName = "users.json";
        private const int MaxRetries = 3;

        // POPRAVEK: Nastavitve za JSON (ignoriraj velike/male črke, lepo oblikovanje)
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        // =======================================================
        // KONSTRUKTOR IN INICIALIZACIJA POTI
        // =======================================================

        public ProfileModel(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;

            // POPRAVEK: Uporabimo ContentRootPath. To je mapa, kjer teče aplikacija.
            _jsonFilePath = Path.Combine(_webHostEnvironment.ContentRootPath, JsonFileName);

            System.Diagnostics.Debug.WriteLine($"[TRACE: INIT] Ciljna pot JSON datoteke: {_jsonFilePath}");
        }

        [BindProperty]
        public InputModel Input { get; set; }

        [BindProperty]
        public IFormFile ProfilePicture { get; set; }

        public string ExistingProfilePicturePath { get; set; } = "/images/default_profile.png";

        public class InputModel
        {
            public string FullName { get; set; }
            public string Email { get; set; }
            public string PhoneNumber { get; set; }
        }

        // =======================================================
        // NALOGA: PRIKAZ STRANI (GET)
        // =======================================================

        public void OnGet()
        {
            var currentUsername = User.FindFirst(ClaimTypes.Name)?.Value;
            System.Diagnostics.Debug.WriteLine($"[TRACE: ONGET] Prijavljen uporabnik: {currentUsername ?? "NI PRIJAVLJEN"}");

            if (string.IsNullOrEmpty(currentUsername))
            {
                Response.Redirect("/Login");
                return;
            }

            var user = LoadUserProfile(currentUsername);
            if (user != null)
            {
                Input = new InputModel
                {
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber
                };
                ExistingProfilePicturePath = !string.IsNullOrEmpty(user.ProfilePicturePath)
                                             ? user.ProfilePicturePath
                                             : "/images/default_profile.png";
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[TRACE: ONGET] Uporabnik '{currentUsername}' ni najden v JSON.");
                Input = new InputModel();
                ModelState.AddModelError(string.Empty, "Napaka: Profil uporabnika ni najden.");
            }
        }

        // =======================================================
        // NALOGA: SHRANJEVANJE PODATKOV (POST)
        // =======================================================

        public async Task<IActionResult> OnPost()
        {
            var currentUsername = User.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(currentUsername))
            {
                return RedirectToPage("/Login");
            }

            if (!ModelState.IsValid)
            {
                var user = LoadUserProfile(currentUsername);
                ExistingProfilePicturePath = user?.ProfilePicturePath ?? "/images/default_profile.png";
                return Page();
            }

            // 1. Obdelava slike (ohrani staro, če ni nove)
            var currentUser = LoadUserProfile(currentUsername);
            string oldPath = currentUser?.ProfilePicturePath ?? "/images/default_profile.png";

            string newPath = await ProcessUploadedFile(oldPath);

            // 2. Shranjevanje v JSON datoteko
            bool success = await SaveProfileChanges(currentUsername, Input, newPath);

            if (success)
            {
                System.Diagnostics.Debug.WriteLine("[TRACE: ONPOST] Shranjevanje USPEŠNO.");
                TempData["Message"] = "Nastavitve profila so bile uspešno shranjene!";
                return RedirectToPage("./Profile");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[TRACE: ONPOST] Shranjevanje NEUSPEŠNO.");
                ModelState.AddModelError(string.Empty, "Shranjevanje ni uspelo. Preverite, ali je datoteka zaklenjena.");
                ExistingProfilePicturePath = newPath;
                return Page();
            }
        }

        // =======================================================
        // LOGIKA DELA Z DATOTEKAMI IN JSON
        // =======================================================

        private UserCredentials LoadUserProfile(string username)
        {
            if (!System.IO.File.Exists(_jsonFilePath))
            {
                System.Diagnostics.Debug.WriteLine($"[TRACE: READ] Datoteka ne obstaja: {_jsonFilePath}");
                return null;
            }

            try
            {
                // Uporabimo FileShare.ReadWrite za manj konfliktov
                using var stream = new FileStream(_jsonFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                var jsonString = reader.ReadToEnd();

                // Deserializacija z našimi opcijami
                var allUsers = JsonSerializer.Deserialize<List<UserCredentials>>(jsonString, _jsonOptions);

                // POPRAVEK: Case-insensitive iskanje uporabnika
                var user = allUsers?.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

                return user;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TRACE: READ ERROR] {ex.Message}");
                return null;
            }
        }

        private async Task<bool> SaveProfileChanges(string username, InputModel input, string profilePicturePath)
        {
            if (!System.IO.File.Exists(_jsonFilePath)) return false;

            try
            {
                // 1. PREBERI CELOTNO DATOTEKO
                string jsonString;
                using (var stream = new FileStream(_jsonFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream))
                {
                    jsonString = await reader.ReadToEndAsync();
                }

                var allUsers = JsonSerializer.Deserialize<List<UserCredentials>>(jsonString, _jsonOptions);

                if (allUsers == null) return false;

                // 2. NAJDI INDEX in POSODOBI (Case-insensitive)
                var userToUpdate = allUsers.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

                if (userToUpdate != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[TRACE: WRITE] Posodabljam uporabnika: {userToUpdate.Username}");

                    userToUpdate.FullName = input.FullName;
                    userToUpdate.Email = input.Email;
                    userToUpdate.PhoneNumber = input.PhoneNumber;
                    userToUpdate.ProfilePicturePath = profilePicturePath;

                    // 3. SERIALIZIRAJ 
                    var updatedJsonString = JsonSerializer.Serialize(allUsers, _jsonOptions);

                    // 4. ZAPIŠI
                    return await WriteFileWithRetry(updatedJsonString);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[TRACE: WRITE] NAPAKA: Uporabnik '{username}' ni najden v seznamu za posodobitev.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TRACE: WRITE ERROR] {ex.Message}");
                return false;
            }
        }

        // Zapis datoteke z možnostjo ponovnega poskusa
        private async Task<bool> WriteFileWithRetry(string content)
        {
            for (int i = 0; i < MaxRetries; i++)
            {
                try
                {
                    // POPRAVEK: FileMode.Create bo povozil obstoječo vsebino z novo
                    using (var stream = new FileStream(_jsonFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (var writer = new StreamWriter(stream))
                    {
                        await writer.WriteAsync(content);
                        System.Diagnostics.Debug.WriteLine("[TRACE: WRITE] Pisanje uspešno.");
                        return true;
                    }
                }
                catch (IOException)
                {
                    System.Diagnostics.Debug.WriteLine($"[TRACE: WRITE RETRY] Datoteka zaklenjena, čakam... ({i + 1}/{MaxRetries})");
                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[TRACE: WRITE FATAL] {ex.Message}");
                    return false;
                }
            }
            return false;
        }

        private async Task<string> ProcessUploadedFile(string existingPath)
        {
            if (ProfilePicture == null || ProfilePicture.Length == 0)
            {
                return existingPath;
            }

            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "profiles");
            string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(ProfilePicture.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            try
            {
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await ProfilePicture.CopyToAsync(fileStream);
                }

                return $"/images/profiles/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TRACE: UPLOAD ERROR] {ex.Message}");
                return existingPath;
            }
        }
    }
}