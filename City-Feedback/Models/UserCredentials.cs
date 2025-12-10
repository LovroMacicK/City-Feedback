using System.Text.Json.Serialization;

namespace City_Feedback.Models
{
    public class UserCredentials
    { 
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string ProfilePicturePath { get; set; }
        public bool DarkMode { get; set; }
        public List<Prijava> Prijave { get; set; }
        public bool IsAdmin { get; set; }
    }
}