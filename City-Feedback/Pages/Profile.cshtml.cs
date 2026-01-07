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
        private const int MaxRetries = 3;
        private const long MaxFileSize = 5 * 1024 * 1024; 

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public ProfileModel(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
            _jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "users.json");
        }

        [BindProperty]
        public InputModel Input { get; set; }

        [BindProperty]
        public IFormFile ProfilePicture { get; set; }

        public string ExistingProfilePicturePath { get; set; } = "/images/default_profile.png";
        public bool DarkMode { get; set; }

        [BindProperty]
        public bool IsPublicProfile { get; set; }

        public class InputModel
        {
            public string FullName { get; set; }
            public string Email { get; set; }
            public string PhoneNumber { get; set; }
            public string CountryCode { get; set; }
            public string Obcina { get; set; }
        }

        public bool IsAdmin { get; set; }
        public List<string> SlovenskeObcine { get; set; } = new List<string>
        {
            "Ajdovščina", "Ankaran", "Apače", "Beltinci", "Benedikt", "Bistrica ob Sotli",
            "Bled", "Bloke", "Bohinj", "Borovnica", "Bovec", "Braslovče", "Brda",
            "Brezovica", "Brežice", "Cankova", "Celje", "Cerklje na Gorenjskem",
            "Cerknica", "Cerkno", "Cerkvenjak", "Cirkulane", "Črenšovci", "Črna na Koroškem",
            "Črnomelj", "Destrnik", "Divača", "Dobje", "Dobrepolje", "Dobrna",
            "Dobrova-Polhov Gradec", "Dobrovnik", "Dol pri Ljubljani", "Dolenjske Toplice",
            "Domžale", "Dornava", "Dravograd", "Duplek", "Gorenja vas-Poljane", "Gorišnica",
            "Gorje", "Gornja Radgona", "Gornji Grad", "Gornji Petrovci", "Grad", "Grosuplje",
            "Hajdina", "Hoče-Slivnica", "Hodoš", "Horjul", "Hrastnik", "Hrpelje-Kozina",
            "Idrija", "Ig", "Ilirska Bistrica", "Ivančna Gorica", "Izola", "Jesenice",
            "Jezersko", "Juršinci", "Kamnik", "Kanal", "Kidričevo", "Kobarid", "Kobilje",
            "Kočevje", "Komen", "Komenda", "Koper", "Kostanjevica na Krki", "Kostel",
            "Kozje", "Kranj", "Kranjska Gora", "Križevci", "Kungota", "Kuzma", "Laško",
            "Lenart", "Lendava", "Litija", "Ljubljana", "Ljubno", "Ljutomer", "Log-Dragomer",
            "Logatec", "Loška dolina", "Loški Potok", "Lovrenc na Pohorju", "Luče", "Lukovica",
            "Majšperk", "Makole", "Maribor", "Markovci", "Medvode", "Mengeš", "Metlika",
            "Mežica", "Miklavž na Dravskem polju", "Miren-Kostanjevica", "Mirna", "Mirna Peč",
            "Mislinja", "Mokronog-Trebelno", "Moravče", "Moravske Toplice", "Mozirje",
            "Murska Sobota", "Muta", "Naklo", "Nazarje", "Nova Gorica", "Novo Mesto",
            "Odranci", "Oplotnica", "Ormož", "Osilnica", "Pesnica", "Piran", "Pivka",
            "Podčetrtek", "Podlehnik", "Podvelka", "Poljčane", "Polzela", "Postojna",
            "Prebold", "Preddvor", "Prevalje", "Ptuj", "Puconci", "Rače-Fram", "Radeče",
            "Radenci", "Radlje ob Dravi", "Radovljica", "Ravne na Koroškem", "Razkrižje",
            "Rečica ob Savinji", "Renče-Vogrsko", "Ribnica", "Ribnica na Pohorju", "Rogaška Slatina",
            "Rogašovci", "Rogatec", "Ruše", "Šalovci", "Selnica ob Dravi", "Semič",
            "Šempeter-Vrtojba", "Šenčur", "Šentilj", "Šentjernej", "Šentjur", "Šentrupert",
            "Sevnica", "Sežana", "Škocjan", "Škofja Loka", "Škofljica", "Slovenj Gradec",
            "Slovenska Bistrica", "Slovenske Konjice", "Šmarje pri Jelšah", "Šmarješke Toplice",
            "Šmartno ob Paki", "Šmartno pri Litiji", "Sodražica", "Solčava", "Šoštanj",
            "Središče ob Dravi", "Starše", "Štore", "Straža", "Sveta Ana", "Sveta Trojica v Slovenskih goricah",
            "Sveti Andraž v Slovenskih goricah", "Sveti Jurij ob Ščavnici", "Sveti Jurij v Slovenskih goricah",
            "Sveti Tomaž", "Tabor", "Tišina", "Tolmin", "Trbovlje", "Trebnje", "Trnovska vas",
            "Tržič", "Trzin", "Turnišče", "Velenje", "Velika Polana", "Velike Lašče",
            "Veržej", "Videm", "Vipava", "Vitanje", "Vodice", "Vojnik", "Vransko",
            "Vrhnika", "Vuzenica", "Zagorje ob Savi", "Žalec", "Zavrč", "Železniki",
            "Žetale", "Žiri", "Žirovnica", "Zreče", "Žužemberk"
        };

        public void OnGet()
        {
            var currentUsername = User.FindFirst(ClaimTypes.Name)?.Value;

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
                    PhoneNumber = user.PhoneNumber,
                    CountryCode = user.CountryCode ?? "+386",
                    Obcina = user.Obcina
                };
                ExistingProfilePicturePath = !string.IsNullOrEmpty(user.ProfilePicturePath)
                                             ? user.ProfilePicturePath
                                             : "/images/default_profile.png";
                DarkMode = user.DarkMode;
                IsAdmin = user.IsAdmin;
                IsPublicProfile = user.IsPublicProfile;
            }
            else
            {
                Input = new InputModel();
                ModelState.AddModelError(string.Empty, "Napaka: Profil uporabnika ni najden.");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var currentUsername = User.FindFirst(ClaimTypes.Name)?.Value;

                if (string.IsNullOrEmpty(currentUsername))
                {
                    return RedirectToPage("/Login");
                }

                ModelState.Remove(nameof(ProfilePicture));

                if (ProfilePicture != null && ProfilePicture.Length > MaxFileSize)
                {
                    ModelState.AddModelError(nameof(ProfilePicture), $"Slika ne sme biti večja od {MaxFileSize / 1024 / 1024} MB.");
                }

                if (ProfilePicture != null && ProfilePicture.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var extension = Path.GetExtension(ProfilePicture.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError(nameof(ProfilePicture), "Dovoljene so samo slike (.jpg, .jpeg, .png, .gif).");
                    }
                }

                if (!ModelState.IsValid)
                {
                    var user = LoadUserProfile(currentUsername);
                    ExistingProfilePicturePath = user?.ProfilePicturePath ?? "/images/default_profile.png";
                    DarkMode = user?.DarkMode ?? false;
                    return Page();
                }

                var currentUser = LoadUserProfile(currentUsername);
                string oldPath = currentUser?.ProfilePicturePath ?? "/images/default_profile.png";

                string newPath = await ProcessUploadedFile(oldPath);

                bool success = await SaveProfileChanges(currentUsername, Input, newPath);

                if (success)
                {
                    TempData["Message"] = "Nastavitve profila so bile uspešno shranjene!";
                    return RedirectToPage("./Profile");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Shranjevanje ni uspelo. Poskusite ponovno.");
                    ExistingProfilePicturePath = newPath;
                    DarkMode = currentUser?.DarkMode ?? false;
                    return Page();
                }
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "Prišlo je do napake pri shranjevanju.");

                var currentUsername = User.FindFirst(ClaimTypes.Name)?.Value;
                if (!string.IsNullOrEmpty(currentUsername))
                {
                    var user = LoadUserProfile(currentUsername);
                    ExistingProfilePicturePath = user?.ProfilePicturePath ?? "/images/default_profile.png";
                    DarkMode = user?.DarkMode ?? false;
                    IsAdmin = user?.IsAdmin ?? false;
                }

                return Page();
            }
        }

        public async Task<IActionResult> OnPostToggleDarkModeAsync([FromBody] DarkModeRequest request)
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(username))
            {
                return new JsonResult(new { success = false, message = "Uporabnik ni prijavljen" });
            }

            bool success = await UpdateDarkMode(username, request.DarkMode);

            if (success)
            {
                return new JsonResult(new { success = true, message = "Nastavitev je bila posodobljena" });
            }
            else
            {
                return new JsonResult(new { success = false, message = "Napaka pri posodabljanju" });
            }
        }

        private UserCredentials LoadUserProfile(string username)
        {
            if (!System.IO.File.Exists(_jsonFilePath))
            {
                return null;
            }

            try
            {
                string jsonString = System.IO.File.ReadAllText(_jsonFilePath);

                if (string.IsNullOrWhiteSpace(jsonString))
                    return null;

                var allUsers = JsonSerializer.Deserialize<List<UserCredentials>>(jsonString, _jsonOptions);
                return allUsers?.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return null;
            }
        }

        private async Task<bool> SaveProfileChanges(string username, InputModel input, string profilePicturePath)
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

                    userToUpdate.FullName = input.FullName;
                    userToUpdate.Email = input.Email;
                    userToUpdate.PhoneNumber = input.PhoneNumber;
                    userToUpdate.CountryCode = input.CountryCode;
                    userToUpdate.ProfilePicturePath = profilePicturePath;
                    userToUpdate.IsPublicProfile = IsPublicProfile;
                    
                    if (userToUpdate.IsAdmin)
                    {
                        userToUpdate.Obcina = input.Obcina;
                    }

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

        private async Task<bool> UpdateDarkMode(string username, bool darkMode)
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

                    var user = allUsers.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

                    if (user == null)
                    {
                        return false;
                    }

                    user.DarkMode = darkMode;

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

        private async Task<string> ProcessUploadedFile(string existingPath)
        {
            if (ProfilePicture == null || ProfilePicture.Length == 0)
            {
                return existingPath;
            }

            try
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "profiles");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(ProfilePicture.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await ProfilePicture.CopyToAsync(fileStream);
                }

                return $"/images/profiles/{uniqueFileName}";
            }
            catch
            {
                return existingPath;
            }
        }

        public class DarkModeRequest
        {
            public bool DarkMode { get; set; }
        }
    }
}