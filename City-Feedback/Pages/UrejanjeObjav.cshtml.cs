using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Hosting;
using System.Security.Claims;
using System.Text.Json;
using City_Feedback.Models;

namespace City_Feedback.Pages
{
    public class UrejanjeObjavModel : PageModel
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly string _jsonFilePath;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public UrejanjeObjavModel(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
            _jsonFilePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "users.json");
        }

        [BindProperty]
        public string Title { get; set; }

        [BindProperty]
        public string Description { get; set; }

        [BindProperty]
        public string Category { get; set; }

        [BindProperty]
        public IFormFile NewImage { get; set; }

        public string CurrentImagePath { get; set; }
        public Guid PrijavaId { get; set; }

        public IActionResult OnGet(Guid id)
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToPage("/Login");
            }

            if (!LoadPrijava(username, id))
            {
                TempData["Error"] = "Prijava ne obstaja ali nimate pravic za urejanje.";
                return RedirectToPage("/Pregled");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid id)
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(username))
            {
                TempData["Error"] = "Za urejanje prijave se morate prijaviti.";
                return RedirectToPage("/Login");
            }

            if (string.IsNullOrWhiteSpace(Title))
            {
                ModelState.AddModelError(nameof(Title), "Naslov je obvezen.");
            }

            if (string.IsNullOrWhiteSpace(Description))
            {
                ModelState.AddModelError(nameof(Description), "Opis je obvezen.");
            }

            if (!ModelState.IsValid)
            {
                LoadPrijava(username, id);
                return Page();
            }

            string newImagePath = null;
            if (NewImage != null && NewImage.Length > 0)
            {
                newImagePath = await ProcessUploadedImage();
            }

            bool success = await UpdatePrijava(username, id, newImagePath);

            if (success)
            {
                TempData["Message"] = "Prijava je bila uspešno posodobljena!";
                return RedirectToPage("/Pregled");
            }
            else
            {
                TempData["Error"] = "Napaka pri posodabljanju prijave.";
                LoadPrijava(username, id);
                return Page();
            }
        }

        private bool LoadPrijava(string username, Guid prijavaId)
        {
            if (!System.IO.File.Exists(_jsonFilePath))
            {
                return false;
            }

            try
            {
                string jsonString = System.IO.File.ReadAllText(_jsonFilePath);
                var allUsers = JsonSerializer.Deserialize<List<UserCredentials>>(jsonString, _jsonOptions);

                var user = allUsers?.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
                var prijava = user?.Prijave?.FirstOrDefault(p => p.Id == prijavaId);

                if (prijava == null)
                {
                    return false;
                }

                PrijavaId = prijava.Id;
                Title = prijava.Naslov;
                Description = prijava.Opis;
                Category = prijava.Kategorija;
                CurrentImagePath = prijava.SlikaPot;

                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> UpdatePrijava(string username, Guid prijavaId, string newImagePath)
        {
            if (!System.IO.File.Exists(_jsonFilePath))
            {
                return false;
            }

            try
            {
                string jsonString = await System.IO.File.ReadAllTextAsync(_jsonFilePath);
                var allUsers = JsonSerializer.Deserialize<List<UserCredentials>>(jsonString, _jsonOptions);

                if (allUsers == null)
                {
                    return false;
                }

                var user = allUsers.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
                var prijava = user?.Prijave?.FirstOrDefault(p => p.Id == prijavaId);

                if (prijava == null)
                {
                    return false;
                }

                prijava.Naslov = Title;
                prijava.Opis = Description;
                prijava.Kategorija = Category ?? "Ostalo";

                if (!string.IsNullOrEmpty(newImagePath))
                {
                    prijava.SlikaPot = newImagePath;
                }

                var updatedJsonString = JsonSerializer.Serialize(allUsers, _jsonOptions);
                await System.IO.File.WriteAllTextAsync(_jsonFilePath, updatedJsonString);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> ProcessUploadedImage()
        {
            if (NewImage == null || NewImage.Length == 0)
            {
                return null;
            }

            try
            {
                string uploadsFolder = System.IO.Path.Combine(_webHostEnvironment.WebRootPath, "images", "prijave");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = Guid.NewGuid().ToString() + System.IO.Path.GetExtension(NewImage.FileName);
                string filePath = System.IO.Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await NewImage.CopyToAsync(fileStream);
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
