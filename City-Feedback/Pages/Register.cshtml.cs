using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using City_Feedback.Models;
using System;

namespace City_Feedback.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public RegisterModel(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Uporabniško ime je obvezno.")]
            [Display(Name = "Uporabniško ime")]
            public string Username { get; set; }

            [Required(ErrorMessage = "E-poštni naslov je obvezen.")]
            [EmailAddress(ErrorMessage = "Neveljaven e-poštni naslov.")]
            [Display(Name = "E-poštni naslov")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Geslo je obvezno.")]
            [DataType(DataType.Password)]
            [Display(Name = "Geslo")]
            public string Password { get; set; }

            [Required(ErrorMessage = "Potrditev gesla je obvezna.")]
            [DataType(DataType.Password)]
            [Display(Name = "Potrdi geslo")]
            [Compare("Password", ErrorMessage = "Gesli se ne ujemata.")]
            public string ConfirmPassword { get; set; }
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

            var jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "users.json");
            List<UserCredentials> allUsers = new List<UserCredentials>();

            if (System.IO.File.Exists(jsonFilePath))
            {
                string jsonString = await System.IO.File.ReadAllTextAsync(jsonFilePath);
                try
                {
                    allUsers = JsonSerializer.Deserialize<List<UserCredentials>>(jsonString) ?? new List<UserCredentials>();
                }
                catch (JsonException)
                {
                    allUsers = new List<UserCredentials>();
                }
            }

            if (allUsers.Any(u => u.Username.Equals(Input.Username, StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError("Input.Username", "To uporabniško ime je že zasedeno.");
                return Page();
            }

            if (allUsers.Any(u => !string.IsNullOrEmpty(u.Email) && u.Email.Equals(Input.Email, StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError("Input.Email", "Ta e-poštni naslov je že registriran.");
                return Page();
            }

            int newId = allUsers.Any() ? allUsers.Max(u => u.Id) + 1 : 1;

            var newUser = new UserCredentials
            {
                Id = newId,
                Username = Input.Username,
                Email = Input.Email,
                Password = Input.Password
            };

            allUsers.Add(newUser);

            var updatedJsonString = JsonSerializer.Serialize(allUsers, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            await System.IO.File.WriteAllTextAsync(jsonFilePath, updatedJsonString);

            TempData["SuccessMessage"] = "Registracija uspešna! Prosimo, prijavite se.";
            return RedirectToPage("/Login");
        }
    }
}