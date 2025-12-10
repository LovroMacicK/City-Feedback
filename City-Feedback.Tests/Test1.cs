using Microsoft.VisualStudio.TestTools.UnitTesting;
using City_Feedback.Models;
using System.Collections.Generic;

namespace City_Feedback.Tests
{
    [TestClass]
    public class Test1
    {
        [TestMethod]
        public void Test_Prijava_ShraniPodatke()
        {
            // Arrange (Priprava)
            var prijava = new Prijava
            {
                Naslov = "Testna težava",
                Opis = "To je testni opis.",
                Kategorija = "Infrastruktura"
            };

            // Act (Izvedba - tu samo preverjamo, če propertyji delajo)
            string naslov = prijava.Naslov;

            // Assert (Preverjanje)
            Assert.AreEqual("Testna težava", naslov);
            Assert.IsNotNull(prijava.Komentarji); // Preveri, da seznam komentarjev ni null
        }
        // 2. Test: Preveri, ali ima nova prijava pravilne privzete vrednosti
        [TestMethod]
        public void Test_Prijava_PrivzeteVrednosti()
        {
            // Arrange & Act
            var prijava = new Prijava();

            // Assert
            // Preverimo, če je status privzeto 'nereseno' (false)
            Assert.IsFalse(prijava.JeReseno);
            // Preverimo, če je privzeta kategorija "Ostalo" (kot imaš v modelu)
            Assert.AreEqual("Ostalo", prijava.Kategorija);
        }

        // 3. Test: Preveri delovanje seznama komentarjev (BREZ avtorja)
        [TestMethod]
        public void Test_Prijava_DodajanjeKomentarja()
        {
            // Arrange
            var prijava = new Prijava();

            var komentar = new Komentar { Vsebina = "To je testni komentar" };

            // Act
            prijava.Komentarji.Add(komentar);

            // Assert
            // Preverimo, če se je komentar uspešno dodal v seznam
            Assert.AreEqual(1, prijava.Komentarji.Count);
            Assert.AreEqual("To je testni komentar", prijava.Komentarji[0].Vsebina);
        }

        // 4. Test: Preveri ročno spreminjanje števila všečkov
        [TestMethod]
        public void Test_Prijava_SteviloVseckov()
        {
            // Arrange
            var prijava = new Prijava();

            // Act
            prijava.SteviloVseckov = 10;
            prijava.SteviloVseckov += 5;

            // Assert
            Assert.AreEqual(15, prijava.SteviloVseckov);
        }

        // 5. Test: Preveri, da seznam "LikedBy" ni null ob ustvarjanju (preprečevanje napak)
        [TestMethod]
        public void Test_Prijava_LikedBySeznamObstaja()
        {
            // Arrange
            var prijava = new Prijava();

            // Act & Assert
            // To je pomembno, da aplikacija ne 'crkne', če hočemo preveriti, kdo je lajkal
            Assert.IsNotNull(prijava.LikedBy);
            Assert.AreEqual(0, prijava.LikedBy.Count);
        }
    }
}