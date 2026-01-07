using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using City_Feedback.Models;
using System.Linq;

namespace City_Feedback.Pages
{
    public class UserProfileModel : PageModel
    {
        private readonly string _jsonFilePath;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public UserProfileModel()
        {
            _jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "users.json");
        }

        public UserCredentials? ProfileUser { get; set; }
        public List<Prijava> UserPrijave { get; set; } = new List<Prijava>();
        
        public int TotalPrijave { get; set; }
        public int ResolvedPrijave { get; set; }
        public int PendingPrijave { get; set; }
        public int TotalUpvotes { get; set; }
        public int TotalComments { get; set; }

        public IActionResult OnGet(string username)
        {
            // Check if request comes from PodrobnostiPrijave page
            var referer = Request.Headers["Referer"].ToString();
            
            if (string.IsNullOrEmpty(referer) || !referer.Contains("/PodrobnostiPrijave/", StringComparison.OrdinalIgnoreCase))
            {
                // Redirect to index if not coming from PodrobnostiPrijave
                TempData["Error"] = "Dostop do uporabniških profilov je omogo?en samo iz strani s podrobnostmi prijave.";
                return RedirectToPage("/Index");
            }

            if (string.IsNullOrEmpty(username))
            {
                ProfileUser = null;
                return Page();
            }

            ProfileUser = LoadUserProfile(username);

            if (ProfileUser == null)
            {
                return Page();
            }

            // If profile is not public, don't load statistics
            if (!ProfileUser.IsPublicProfile)
            {
                return Page();
            }

            // Load user's prijave
            UserPrijave = ProfileUser.Prijave ?? new List<Prijava>();
            
            // Calculate statistics
            TotalPrijave = UserPrijave.Count;
            ResolvedPrijave = UserPrijave.Count(p => p.JeReseno);
            PendingPrijave = UserPrijave.Count(p => !p.JeReseno);
            TotalUpvotes = UserPrijave.Sum(p => p.Upvotes);
            TotalComments = UserPrijave.Sum(p => p.Komentarji?.Count ?? 0);

            return Page();
        }

        private UserCredentials? LoadUserProfile(string username)
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
                
                // Search by Username first, then by FullName (for legacy data compatibility)
                return allUsers?.FirstOrDefault(u => 
                    u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(u.FullName) && u.FullName.Equals(username, StringComparison.OrdinalIgnoreCase)));
            }
            catch
            {
                return null;
            }
        }
    }
}
