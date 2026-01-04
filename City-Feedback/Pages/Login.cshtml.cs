using City_Feedback.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace City_Feedback.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public LoginModel(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

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
            var jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "users.json");

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

            var authenticatedUser = allUsers?.FirstOrDefault(u =>
                u.Username.Equals(credential.Username, StringComparison.OrdinalIgnoreCase) &&
                u.Password == credential.Password);

            if (authenticatedUser == null)
            {
                ModelState.AddModelError(string.Empty, "Napačno ime ali geslo");
                return Page();
            }

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, authenticatedUser.Username),
    };

            if (authenticatedUser.IsAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
                HttpContext.Session.SetString("JeAdmin", "DA");
            }
            else
            {
                HttpContext.Session.Remove("JeAdmin");
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = false };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties
            );

            if (authenticatedUser.IsAdmin)
                return RedirectToPage("/Obcina");

            return LocalRedirect("/");
        }

    }
}