using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Text.Json;
using City_Feedback.Models;

namespace City_Feedback.Pages
{
    public class PregledModel : PageModel
    {
        private readonly string _jsonFilePath;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public PregledModel()
        {
            _jsonFilePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "users.json");
        }

        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string ProfilePicturePath { get; set; }
        public int TotalPrijave { get; set; }
        public int TotalLikes { get; set; }
        public int ResolvedPrijave { get; set; }
        public List<Prijava> UserPrijave { get; set; } = new List<Prijava>();

        public IActionResult OnGet()
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToPage("/Login");
            }

            LoadUserData(username);
            return Page();
        }

        private void LoadUserData(string username)
        {
            if (!System.IO.File.Exists(_jsonFilePath))
            {
                return;
            }

            try
            {
                string jsonString = System.IO.File.ReadAllText(_jsonFilePath);
                var allUsers = JsonSerializer.Deserialize<List<UserCredentials>>(jsonString, _jsonOptions);

                var currentUser = allUsers?.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

                if (currentUser != null)
                {
                    Username = currentUser.Username;
                    FullName = currentUser.FullName;
                    Email = currentUser.Email;
                    ProfilePicturePath = !string.IsNullOrEmpty(currentUser.ProfilePicturePath)
                        ? currentUser.ProfilePicturePath
                        : "/images/default_profile.png";

                    if (currentUser.Prijave != null)
                    {
                        UserPrijave = currentUser.Prijave.OrderByDescending(p => p.Datum).ToList();
                        TotalPrijave = UserPrijave.Count;
                        TotalLikes = UserPrijave.Sum(p => p.SteviloVseckov);
                        ResolvedPrijave = UserPrijave.Count(p => p.JeReseno);
                    }
                }
            }
            catch
            {
                // V primeru napake uporabi privzete vrednosti
            }
        }
    }
}
