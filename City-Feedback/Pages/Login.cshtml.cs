using City_Feedback.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http; // Potrebno za Session
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq; // Potrebno za .FirstOrDefault
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
            // ---------------------------------------------------------
            // 1. POSEBEN PREHOD ZA OBČINO (Admin)
            // ---------------------------------------------------------
            if (credential.Username == "obcina" && credential.Password == "admin")
            {
                // Nastavimo sejo, da vemo, da je to admin
                HttpContext.Session.SetString("JeAdmin", "DA");

                // Vseeno ga prijavimo v sistem (Cookies), da deluje [Authorize]
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, "Občina Admin"),
                    new Claim(ClaimTypes.Role, "Admin")
                };
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties { IsPersistent = false };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

                // Preusmerimo na admin stran
                return RedirectToPage("/Obcina");
            }

            // ---------------------------------------------------------
            // 2. PREVERJANJE NAVADNIH UPORABNIKOV (users.json)
            // ---------------------------------------------------------
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

            // Preverimo uporabnika v JSON datoteki
            var authenticatedUser = allUsers?.FirstOrDefault(u =>
                u.Username.Equals(credential.Username, StringComparison.OrdinalIgnoreCase) &&
                u.Password == credential.Password);

            if (authenticatedUser != null)
            {
                // Če je uporabnik najden, ustvarimo piškotek (Login)
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, authenticatedUser.Username),
                };
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = false,
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

                // Navadni uporabniki gredo na prvo stran
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