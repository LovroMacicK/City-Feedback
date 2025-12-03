using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using City_Feedback.Models;
using System.IO;

namespace City_Feedback.Pages
{
    public class LoginModel : PageModel
    {
<<<<<<< Updated upstream
<<<<<<< Updated upstream
        private readonly IWebHostEnvironment _webHostEnvironment;
        public LoginModel(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

=======
        string Username { get; set; }
        string Password { get; set; }
>>>>>>> Stashed changes
=======
        string Username { get; set; }
        string Password { get; set; }
>>>>>>> Stashed changes
        [BindProperty]
        public Credential credential { get; set; }

        public class Credential
        {
            [Required(ErrorMessage = "Obvezno vnesite uporabniško ime")]
            public string Username { get; set; }

            [Required(ErrorMessage = "Obvezno vnesite geslo")]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
<<<<<<< Updated upstream
<<<<<<< Updated upstream
            var jsonFilePath = Path.Combine(_webHostEnvironment.ContentRootPath, "users.json");
=======
=======
>>>>>>> Stashed changes
            foreach (var username in usernames)
            {
                if (credential.Username == username && credential.Password == password)
                {
                    return RedirectToPage("/Index");
                }
                else
                {
                        ModelState.AddModelError(string.Empty, "Napačno ime ali geslo");
                        return Page();
                }
            }
            return Page();
        }
>>>>>>> Stashed changes

            if (!System.IO.File.Exists(jsonFilePath))
            {
                ModelState.AddModelError(string.Empty, "Napaka: Datoteka users.json ni najdena.");
                return Page();
            }
            string jsonString = await System.IO.File.ReadAllTextAsync(jsonFilePath);
            List<UserCredentials> allUsers;

            try
            {
                allUsers = JsonSerializer.Deserialize<List<UserCredentials>>(jsonString);
            }
            catch (JsonException)
            {
                ModelState.AddModelError(string.Empty, "Napaka: Neveljavna JSON struktura podatkov.");
                return Page();
            }
            var authenticatedUser = allUsers?.FirstOrDefault(u => u.Username.Equals(credential.Username, StringComparison.OrdinalIgnoreCase) &&u.Password == credential.Password);

            if (authenticatedUser != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, authenticatedUser.Username),
                };
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = false,
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity),authProperties);
                return LocalRedirect("/");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Napačno ime ali geslo");
                return Page();
            }
        }
    }
}