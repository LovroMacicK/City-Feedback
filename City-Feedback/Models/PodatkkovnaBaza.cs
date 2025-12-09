using City_Feedback.Pages;

namespace City_Feedback.Models
{
    public static class PodatkovnaBaza
    {
        public static List<FeedbackItem> Prijave = new List<FeedbackItem>();

        static PodatkovnaBaza()
        {
            Prijave.Add(new FeedbackItem
            {
                Id = Guid.NewGuid(),
                Naslov = "Luknja na cesti",
                Opis = "Nevarna luknja na glavni cesti.",
                Datum = DateTime.Now.AddDays(-2),
                SteviloVseckov = 10,
                JeReseno = false
            });
        }
    }
}