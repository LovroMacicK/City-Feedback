namespace City_Feedback.Models
{
    public class FeedbackItem
    {
        public Guid Id { get; set; }
        public string Naslov { get; set; }
        public string Opis { get; set; }
        public DateTime Datum { get; set; }
        public string? SlikaPot { get; set; }
        public int SteviloVseckov { get; set; }
        public bool JeReseno { get; set; }
    }
}