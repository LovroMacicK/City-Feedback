using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using City_Feedback.Models;

namespace City_Feedback.Pages
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly string _jsonFilePath;
        private const int MaxRetries = 3;

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public ForgotPasswordModel()
        {
            _jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "users.json");
        }

        [BindProperty]
        [Required(ErrorMessage = "E-poštni naslov je obvezen.")]
        [EmailAddress(ErrorMessage = "Neveljaven e-poštni naslov.")]
        public string Email { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await FindUserByEmail(Email);

            if (user == null)
            {
                // Security: Don't reveal if email exists
                TempData["Message"] = "?e je ta e-poštni naslov registriran, boste prejeli povezavo za ponastavitev gesla.";
                return Page();
            }

            // Generate reset token
            var resetToken = Guid.NewGuid().ToString("N");
            var tokenExpiry = DateTime.Now.AddHours(24);

            bool success = await SaveResetToken(user.Username, resetToken, tokenExpiry);

            if (success)
            {
                // In production, send email here
                // For now, we'll show the reset link directly
                var resetLink = $"{Request.Scheme}://{Request.Host}/ResetPassword?token={resetToken}";
                
                TempData["Message"] = $"Povezava za ponastavitev gesla: <a href='{resetLink}' class='alert-link'>{resetLink}</a>";
            }
            else
            {
                TempData["Error"] = "Prišlo je do napake. Poskusite ponovno.";
            }

            return Page();
        }

        private async Task<UserCredentials> FindUserByEmail(string email)
        {
            if (!System.IO.File.Exists(_jsonFilePath))
            {
                return null;
            }

            try
            {
                string jsonString = await System.IO.File.ReadAllTextAsync(_jsonFilePath);
                var allUsers = JsonSerializer.Deserialize<List<UserCredentials>>(jsonString, _jsonOptions);

                return allUsers?.FirstOrDefault(u => 
                    !string.IsNullOrEmpty(u.Email) && 
                    u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return null;
            }
        }

        private async Task<bool> SaveResetToken(string username, string token, DateTime expiry)
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

                    user.ResetToken = token;
                    user.ResetTokenExpiry = expiry;

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
