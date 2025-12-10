namespace City_Feedback.Models
{
    public class Komentar
    {
        public Guid Id { get; set; }
        public string Vsebina { get; set; }
        public DateTime Datum { get; set; }
        public string Username { get; set; }
        public string UserFullName { get; set; }
        public string UserProfilePicture { get; set; }
    }
}