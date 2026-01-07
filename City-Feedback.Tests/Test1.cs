using Microsoft.VisualStudio.TestTools.UnitTesting;
using City_Feedback.Models;
using System.Collections.Generic;

namespace City_Feedback.Tests
{
    [TestClass]
    public class BasicPrijavaTests
    {
        [TestMethod]
        public void Test_Prijava_ShraniPodatke()
        {
            var prijava = new Prijava
            {
                Naslov = "Testna težava",
                Opis = "To je testni opis.",
                Kategorija = "Infrastruktura"
            };

            string naslov = prijava.Naslov;

            Assert.AreEqual("Testna težava", naslov);
            Assert.IsNotNull(prijava.Komentarji);
        }

        [TestMethod]
        public void Test_Prijava_PrivzeteVrednosti()
        {
            var prijava = new Prijava();

            Assert.IsFalse(prijava.JeReseno);
            Assert.AreEqual("Ostalo", prijava.Kategorija);
        }

        [TestMethod]
        public void Test_Prijava_DodajanjeKomentarja()
        {
            var prijava = new Prijava();

            var komentar = new Komentar { Vsebina = "To je testni komentar" };

            prijava.Komentarji.Add(komentar);

            Assert.AreEqual(1, prijava.Komentarji.Count);
            Assert.AreEqual("To je testni komentar", prijava.Komentarji[0].Vsebina);
        }

        [TestMethod]
        public void Test_Prijava_SteviloVseckov()
        {
            var prijava = new Prijava();

            prijava.SteviloVseckov = 10;
            prijava.SteviloVseckov += 5;

            Assert.AreEqual(15, prijava.SteviloVseckov);
        }

        [TestMethod]
        public void Test_Prijava_LikedBySeznamObstaja()
        {
            var prijava = new Prijava();

            Assert.IsNotNull(prijava.LikedBy);
            Assert.AreEqual(0, prijava.LikedBy.Count);
        }
    }
}