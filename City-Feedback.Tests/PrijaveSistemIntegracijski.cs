using Microsoft.VisualStudio.TestTools.UnitTesting;
using City_Feedback.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace City_Feedback.Tests
{
    [TestClass]
    public class PrijaveSistemIntegracijski
    {
        private string testJsonPath;
        private JsonSerializerOptions jsonOptions;

        [TestInitialize]
        public void Priprava()
        {
            testJsonPath = Path.Combine(Path.GetTempPath(), $"test_prijave_{Guid.NewGuid()}.json");
            
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
        public void NovaPrijava_DodajanjeUporabniku_UspesenZapis()
        {
            var uporabniki = new List<UserCredentials>
            {
                new UserCredentials
                {
                    Id = 1,
                    Username = "janez",
                    Prijave = new List<Prijava>()
                }
            };

            var novaPrijava = new Prijava
            {
                Id = Guid.NewGuid(),
                Naslov = "Poškodovana cesta na Celovški",
                Opis = "Velika luknja na cestišču",
                Kategorija = "Ceste",
                Obcina = "Ljubljana",
                Lokacija = "Celovška cesta 150",
                JeReseno = false
            };

            uporabniki[0].Prijave.Add(novaPrijava);

            var jsonString = JsonSerializer.Serialize(uporabniki, jsonOptions);
            File.WriteAllText(testJsonPath, jsonString);

            var prebranJsonString = File.ReadAllText(testJsonPath);
            var prebranUporabniki = JsonSerializer.Deserialize<List<UserCredentials>>(prebranJsonString, jsonOptions);

            Assert.AreEqual(1, prebranUporabniki[0].Prijave.Count);
            Assert.AreEqual("Poškodovana cesta na Celovški", prebranUporabniki[0].Prijave[0].Naslov);
            Assert.IsFalse(prebranUporabniki[0].Prijave[0].JeReseno);
        }

        [TestMethod]
        public void OznaciPrijavoKotReseno_PosodobiStatus_SpremembaShranjenaVJson()
        {
            var uporabniki = new List<UserCredentials>
            {
                new UserCredentials
                {
                    Id = 1,
                    Username = "testni",
                    Prijave = new List<Prijava>
                    {
                        new Prijava
                        {
                            Id = Guid.NewGuid(),
                            Naslov = "Za reševanje",
                            JeReseno = false
                        }
                    }
                }
            };

            var jsonString = JsonSerializer.Serialize(uporabniki, jsonOptions);
            File.WriteAllText(testJsonPath, jsonString);

            var prebranJsonString = File.ReadAllText(testJsonPath);
            var prebranUporabniki = JsonSerializer.Deserialize<List<UserCredentials>>(prebranJsonString, jsonOptions);
            
            prebranUporabniki[0].Prijave[0].JeReseno = true;

            var posodobljeniJsonString = JsonSerializer.Serialize(prebranUporabniki, jsonOptions);
            File.WriteAllText(testJsonPath, posodobljeniJsonString);

            var koncniJsonString = File.ReadAllText(testJsonPath);
            var koncniUporabniki = JsonSerializer.Deserialize<List<UserCredentials>>(koncniJsonString, jsonOptions);

            Assert.IsTrue(koncniUporabniki[0].Prijave[0].JeReseno);
        }
    }
}

