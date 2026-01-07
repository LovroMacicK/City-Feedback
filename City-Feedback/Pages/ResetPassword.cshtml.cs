using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using City_Feedback.Models;

namespace City_Feedback.Pages
{
    public class ResetPasswordModel : PageModel
    {
        private readonly string _jsonFilePath;
        private const int MaxRetries = 3;

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public ResetPasswordModel()
        {
            _jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "users.json");
        }

        [BindProperty(SupportsGet = true)]
        public string Token { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Novo geslo je obvezno.")]
        [MinLength(6, ErrorMessage = "Geslo mora biti dolgo vsaj 6 znakov.")]
        public string NewPassword { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Potrditev gesla je obvezna.")]
        [Compare("NewPassword", ErrorMessage = "Gesli se ne ujemata.")]
        public string ConfirmPassword { get; set; }

        public bool IsValidToken { get; set; }

        public async Task OnGetAsync()
        {
            if (string.IsNullOrEmpty(Token))
            {
                IsValidToken = false;
                return;
            }

            IsValidToken = await ValidateToken(Token);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Token))
            {
                TempData["Error"] = "Neveljaven token.";
                return RedirectToPage("/ForgotPassword");
            }

            if (!ModelState.IsValid)
            {
                IsValidToken = await ValidateToken(Token);
                return Page();
            }

            bool success = await ResetUserPassword(Token, NewPassword);

            if (success)
            {
                TempData["Message"] = "Geslo je bilo uspešno spremenjeno. Sedaj se lahko prijavite z novim geslom.";
                return RedirectToPage("/Login");
            }
            else
            {
                TempData["Error"] = "Povezava za ponastavitev je neveljavna ali je potekla.";
                IsValidToken = false;
                return Page();
            }
        }

        private async Task<bool> ValidateToken(string token)
        {
            if (!System.IO.File.Exists(_jsonFilePath))
            {
                return false;
            }

            try
            {
                string jsonString = await System.IO.File.ReadAllTextAsync(_jsonFilePath);
                var allUsers = JsonSerializer.Deserialize<List<UserCredentials>>(jsonString, _jsonOptions);

                var user = allUsers?.FirstOrDefault(u => 
                    u.ResetToken == token && 
                    u.ResetTokenExpiry.HasValue && 
                    u.ResetTokenExpiry.Value > DateTime.Now);

                return user != null;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> ResetUserPassword(string token, string newPassword)
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

                    var user = allUsers.FirstOrDefault(u => 
                        u.ResetToken == token && 
                        u.ResetTokenExpiry.HasValue && 
                        u.ResetTokenExpiry.Value > DateTime.Now);

                    if (user == null)
                    {
                        return false;
                    }

                    // Update password and clear reset token
                    user.Password = newPassword;
                    user.ResetToken = null;
                    user.ResetTokenExpiry = null;

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
    }
}
