using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace City_Feedback.Pages
{
    public class LoginModel : PageModel
    {
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
        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
            string[] usernames = { "Admin", "User" };
            string password = "geslo";
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

    }
}
