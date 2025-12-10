using City_Feedback.Models; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace City_Feedback.Pages
{
    public class ObcinaModel : PageModel
    {
        public List<Prijava> PrijaveZaObcino { get; set; }

        public IActionResult OnGet()
        {
            // TUKAJ PREVERIŠ, ÈE JE UPORABNIK OBÈINA (Admin)
            // Primer: if (User.Identity.Name != "obcina@email.com") return RedirectToPage("/Login");

            // Sortiramo: Najprej nerešene, nato po številu všeèkov
            PrijaveZaObcino = PodatkovnaBaza.Prijave
                .OrderBy(x => x.JeReseno) // Nerešene (false) gredo na vrh
                .ThenByDescending(x => x.SteviloVseckov) // Najbolj všeèkane na vrh
                .ToList();

            return Page();
        }

        public IActionResult OnPostOznaciKotReseno(Guid id)
        {
            var prijava = PodatkovnaBaza.Prijave.FirstOrDefault(x => x.Id == id);
            if (prijava != null)
            {
                prijava.JeReseno = true;
            }
            return RedirectToPage();
        }
    }
}