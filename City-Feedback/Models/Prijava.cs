using System.Text.Json.Serialization;

namespace City_Feedback.Models
{
    public class Prijava
    {
        public Guid Id { get; set; }
        public string Naslov { get; set; }
        public string Opis { get; set; }
        public DateTime Datum { get; set; }
        public string? SlikaPot { get; set; }
        public int SteviloVseckov { get; set; }
        public bool JeReseno { get; set; }
        public List<string> LikedBy { get; set; } = new List<string>();
        public string OwnerUsername { get; set; }
        public string OwnerProfilePicture { get; set; }
        public string Kategorija { get; set; } = "Ostalo";

        public DateTime LastUpdated { get; set; } = DateTime.Now;

        public List<Komentar> Komentarji { get; set; } = new List<Komentar>();


    }
}