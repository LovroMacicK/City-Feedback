using System.Text.Json.Serialization;

namespace City_Feedback.Models
{
    public enum NivoPrioritet
    {
        KoBoPoslovno = 0,
        Pomembno = 1,
        Nujno = 2
    }
    
    public class Prijava
    {
        public Guid Id { get; set; }
        public string Naslov { get; set; }
        public string Opis { get; set; }
        public DateTime Datum { get; set; }
        public string? SlikaPot { get; set; }
        

        public int Upvotes { get; set; }
        public int Downvotes { get; set; }
        public List<string> UpvotedBy { get; set; } = new List<string>();
        public List<string> DownvotedBy { get; set; } = new List<string>();
        
        public int SteviloVseckov { get; set; }
        public List<string> LikedBy { get; set; } = new List<string>();
        
        public bool JeReseno { get; set; }
        public bool JeArhivirano { get; set; } = false;
        public NivoPrioritet Nujnost { get; set; } = NivoPrioritet.KoBoPoslovno;
        
        public string OwnerUsername { get; set; }
        public string OwnerProfilePicture { get; set; }
        public string Kategorija { get; set; } = "Ostalo";
        public string? Obcina { get; set; }
        public string? Lokacija { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.Now;

        public List<Komentar> Komentarji { get; set; } = new List<Komentar>();


    }
}