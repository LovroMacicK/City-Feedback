using System.Text.Json.Serialization;

namespace City_Feedback.Models
{
    public class UserCredentials
    {
            public string Username { get; set; }
            public string Password { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
            public string PhoneNumber { get; set; }
            public string ProfilePicturePath { get; set; }
    }
}