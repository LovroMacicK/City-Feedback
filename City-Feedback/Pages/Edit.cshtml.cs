using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using City_Feedback.Models;
using System.Security.Claims;
using System.Text.Json;

namespace City_Feedback.Pages
{
    public class EditModel : PageModel
    {
        private readonly IWebHostEnvironment _env;
        private readonly string _dbPath;

        public EditModel(IWebHostEnvironment env)
        {
            _env = env;
            _dbPath = Path.Combine(Directory.GetCurrentDirectory(), "users.json");
        }

        [BindProperty]
        public Prijava Input { get; set; }

        [BindProperty]
        public IFormFile NewImage { get; set; }

        public IActionResult OnGet(Guid id)
        {
            var prijava = LoadPrijava(id);

            if (prijava == null)
                return RedirectToPage("/Index");

            // dovolimo urejanje samo lastniku
            if (User.Identity?.Name != prijava.OwnerUsername)
                return Unauthorized();

            Input = prijava;
            return Page();
        }

        public IActionResult OnPost()
        {
            var prijava = LoadPrijava(Input.Id);

            if (prijava == null)
                return RedirectToPage("/Index");

            if (User.Identity?.Name != prijava.OwnerUsername)
                return Unauthorized();

            prijava.Naslov = Input.Naslov;
            prijava.Opis = Input.Opis;
            prijava.Kategorija = Input.Kategorija;
            prijava.LastUpdated = DateTime.Now;

            // če je nova slika naložena
            if (NewImage != null)
            {
                string folder = Path.Combine(_env.WebRootPath, "images/prijave");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string newName = Guid.NewGuid() + Path.GetExtension(NewImage.FileName);
                string filePath = Path.Combine(folder, newName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                    NewImage.CopyTo(stream);

                prijava.SlikaPot = $"/images/prijave/{newName}";
            }

            SavePrijava(prijava);

            TempData["Message"] = "Prijava je bila uspešno posodobljena!";
            return RedirectToPage("/Index");
        }

        // 🗑 odstrani sliko
        public IActionResult OnPostRemoveImage(Guid id)
        {
            var prijava = LoadPrijava(id);

            if (prijava == null)
                return RedirectToPage("/Index");

            if (User.Identity?.Name != prijava.OwnerUsername)
                return Unauthorized();

            prijava.SlikaPot = null;
            prijava.LastUpdated = DateTime.Now;

            SavePrijava(prijava);

            TempData["Message"] = "Slika odstranjena!";
            return RedirectToPage("/Edit", new { id });
        }

        // ===== Helper funkcije =====

        private Prijava LoadPrijava(Guid id)
        {
            if (!System.IO.File.Exists(_dbPath)) return null;

            var json = System.IO.File.ReadAllText(_dbPath);
            var users = JsonSerializer.Deserialize<List<UserCredentials>>(json);

            return users?
                .SelectMany(u => u.Prijave ?? new List<Prijava>())
                .FirstOrDefault(p => p.Id == id);
        }

        private void SavePrijava(Prijava updated)
        {
            var json = System.IO.File.ReadAllText(_dbPath);
            var users = JsonSerializer.Deserialize<List<UserCredentials>>(json);

            foreach (var u in users)
            {
                if (u.Prijave == null) continue;

                var index = u.Prijave.FindIndex(p => p.Id == updated.Id);
                if (index >= 0)
                {
                    u.Prijave[index] = updated;
                    break;
                }
            }

            var newJson = JsonSerializer.Serialize(users, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            System.IO.File.WriteAllText(_dbPath, newJson);
        }
    }
}
