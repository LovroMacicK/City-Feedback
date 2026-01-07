using Microsoft.VisualStudio.TestTools.UnitTesting;
using City_Feedback.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace City_Feedback.Tests
{
    [TestClass]
    public class KomentarjiSistemIntegracijski
    {
        private string testJsonPath;
        private JsonSerializerOptions jsonOptions;

        [TestInitialize]
        public void Priprava()
        {
            testJsonPath = Path.Combine(Path.GetTempPath(), $"test_komentarji_{Guid.NewGuid()}.json");
            
            jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
        }

        [TestCleanup]
        public void Ciscenje()
        {
            if (File.Exists(testJsonPath))
            {
                File.Delete(testJsonPath);
            }
        }

        [TestMethod]
        public void DodajKomentar_NaPrijavo_ShranjenaVJson()
        {
            var uporabniki = new List<UserCredentials>
            {
                new UserCredentials
                {
                    Id = 1,
                    Username = "lastnik",
                    Prijave = new List<Prijava>
                    {
                        new Prijava
                        {
                            Id = Guid.NewGuid(),
                            Naslov = "Test prijava",
                            Komentarji = new List<Komentar>()
                        }
                    }
                }
            };

            var jsonString = JsonSerializer.Serialize(uporabniki, jsonOptions);
            File.WriteAllText(testJsonPath, jsonString);

            var prebranJsonString = File.ReadAllText(testJsonPath);
            var prebranUporabniki = JsonSerializer.Deserialize<List<UserCredentials>>(prebranJsonString, jsonOptions);

            var noviKomentar = new Komentar
            {
                Id = Guid.NewGuid(),
                Vsebina = "To je testni komentar",
                Username = "komentator",
                Datum = DateTime.Now
            };

            prebranUporabniki[0].Prijave[0].Komentarji.Add(noviKomentar);

            var posodobljeniJsonString = JsonSerializer.Serialize(prebranUporabniki, jsonOptions);
            File.WriteAllText(testJsonPath, posodobljeniJsonString);

            var koncniJsonString = File.ReadAllText(testJsonPath);
            var koncniUporabniki = JsonSerializer.Deserialize<List<UserCredentials>>(koncniJsonString, jsonOptions);

            Assert.AreEqual(1, koncniUporabniki[0].Prijave[0].Komentarji.Count);
            Assert.AreEqual("To je testni komentar", koncniUporabniki[0].Prijave[0].Komentarji[0].Vsebina);
        }
    }
}

