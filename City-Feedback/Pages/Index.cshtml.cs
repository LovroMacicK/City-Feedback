using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace City_Feedback.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IWebHostEnvironment _environment; // To nam omogoèa dostop do mape wwwroot

        // STATIC seznam
        private static List<FeedbackItem> _vsePrijave = new List<FeedbackItem>();

        [BindProperty]
        public string Title { get; set; }

        [BindProperty]
        public string Description { get; set; }

        [BindProperty]
        public IFormFile? Image { get; set; } // Tukaj dobimo datoteko iz obrazca

        public List<FeedbackItem> Prijave { get; set; }

        // V konstruktor dodamo 'IWebHostEnvironment environment'
        public IndexModel(ILogger<IndexModel> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
            Prijave = _vsePrijave;
        }

        public void OnGet()
        {
            if (_vsePrijave.Count == 0)
            {
                // Testni podatki
                _vsePrijave.Add(new FeedbackItem
                {
                    Naslov = "Luknja na cesti",
                    Opis = "Velika luknja na Prešernovi cesti.",
                    Datum = DateTime.Now.AddDays(-1),
                    SlikaPot = null // Ni slike
                });
            }

            Prijave = _vsePrijave.OrderByDescending(x => x.Datum).ToList();
        }

        public async Task<IActionResult> OnPostAsync() // Spremenimo v Async zaradi shranjevanja datoteke
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            string? shranjenaPotSlike = null;

            // 1. Preverimo, èe je uporabnik naložil sliko
            if (Image != null)
            {
                // Ustvarimo mapo 'uploads', èe še ne obstaja
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generiramo unikatno ime datoteke (da se ne prepišejo slike z istim imenom)
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Image.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Shranimo datoteko na disk
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await Image.CopyToAsync(fileStream);
                }

                // Pripravimo pot za splet (brskalnik vidi mapo wwwroot kot root '/')
                shranjenaPotSlike = "/uploads/" + uniqueFileName;
            }

            // 2. Ustvarimo novo prijavo
            var novaPrijava = new FeedbackItem
            {
                Naslov = Title,
                Opis = Description,
                Datum = DateTime.Now,
                SlikaPot = shranjenaPotSlike // Shranimo pot do slike
            };

            _vsePrijave.Add(novaPrijava);

            return RedirectToPage();
        }
    }

    public class FeedbackItem
    {
        public string Naslov { get; set; }
        public string Opis { get; set; }
        public DateTime Datum { get; set; }
        public string? SlikaPot { get; set; } // Novo polje za pot do slike
    }
}