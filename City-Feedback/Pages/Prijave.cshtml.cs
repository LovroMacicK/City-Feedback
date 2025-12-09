using City_Feedback.Models; // Nujno, da najde PodatkovnaBaza
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace City_Feedback.Pages
{
    public class PrijaveModel : PageModel
    {
        private readonly ILogger<PrijaveModel> _logger;
        private readonly IWebHostEnvironment _environment;

        [BindProperty]
        public string Title { get; set; }

        [BindProperty]
        public string Description { get; set; }

        [BindProperty]
        public IFormFile? Image { get; set; }

        public List<FeedbackItem> Prijave { get; set; }

        public PrijaveModel(ILogger<PrijaveModel> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
            // POPRAVEK 1: Povezava na skupno bazo
            Prijave = PodatkovnaBaza.Prijave;
        }

        public void OnGet(string sortOrder)
        {
            ViewData["CurrentSort"] = sortOrder;

            // POPRAVEK 2: Uporaba skupne baze
            var vsiPodatki = PodatkovnaBaza.Prijave;

            switch (sortOrder)
            {
                case "likes":
                    Prijave = vsiPodatki.OrderByDescending(x => x.SteviloVseckov).ToList();
                    break;
                default:
                    Prijave = vsiPodatki.OrderByDescending(x => x.Datum).ToList();
                    break;
            }
        }

        public IActionResult OnPostVote(Guid id)
        {
            // POPRAVEK 3: Iskanje v skupni bazi
            var objava = PodatkovnaBaza.Prijave.FirstOrDefault(x => x.Id == id);

            if (objava != null)
            {
                objava.SteviloVseckov++;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            string? shranjenaPotSlike = null;

            if (Image != null)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Image.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await Image.CopyToAsync(fileStream);
                }

                shranjenaPotSlike = "/uploads/" + uniqueFileName;
            }

            // Preprièaj se, da FeedbackItem bereš iz Models (ne podvajaj razreda spodaj)
            var novaPrijava = new FeedbackItem
            {
                Id = Guid.NewGuid(),
                Naslov = Title,
                Opis = Description,
                Datum = DateTime.Now,
                SlikaPot = shranjenaPotSlike,
                SteviloVseckov = 0,
                JeReseno = false
            };

            // POPRAVEK 4: Dodajanje v skupno bazo
            PodatkovnaBaza.Prijave.Add(novaPrijava);

            return RedirectToPage();
        }
    }
}