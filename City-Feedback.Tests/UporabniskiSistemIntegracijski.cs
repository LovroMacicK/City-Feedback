using Microsoft.VisualStudio.TestTools.UnitTesting;
using City_Feedback.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace City_Feedback.Tests
{
    [TestClass]
    public class UporabniskiSistemIntegracijski
    {
        private string testJsonPath;
        private JsonSerializerOptions jsonOptions;

        [TestInitialize]
        public void Priprava()
        {
            testJsonPath = Path.Combine(Path.GetTempPath(), $"test_users_{Guid.NewGuid()}.json");
            
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
        public void NoviUporabnik_ShranjevVJson_UspesenZapis()
        {
            var uporabniki = new List<UserCredentials>
            {
                new UserCredentials
                {
                    Id = 1,
                    Username = "testni_uporabnik",
                    Password = "geslo123",
                    FullName = "Testni Uporabnik",
                    Email = "test@primer.si",
                    PhoneNumber = "040123456",
                    CountryCode = "+386"
                }
            };

            var jsonString = JsonSerializer.Serialize(uporabniki, jsonOptions);
            File.WriteAllText(testJsonPath, jsonString);

            var prebranJsonString = File.ReadAllText(testJsonPath);
            var prebranUporabniki = JsonSerializer.Deserialize<List<UserCredentials>>(prebranJsonString, jsonOptions);

            Assert.IsNotNull(prebranUporabniki);
            Assert.AreEqual(1, prebranUporabniki.Count);
            Assert.AreEqual("testni_uporabnik", prebranUporabniki[0].Username);
            Assert.AreEqual("test@primer.si", prebranUporabniki[0].Email);
        }

        [TestMethod]
        public void PosodobiUporabnika_ShranjevVJson_SpremembeShranjene()
        {
            var uporabniki = new List<UserCredentials>
            {
                new UserCredentials
                {
                    Id = 1,
                    Username = "testni",
                    FullName = "Staro Ime",
                    DarkMode = false
                }
            };

            var jsonString = JsonSerializer.Serialize(uporabniki, jsonOptions);
            File.WriteAllText(testJsonPath, jsonString);

            var prebranJsonString = File.ReadAllText(testJsonPath);
            var prebranUporabniki = JsonSerializer.Deserialize<List<UserCredentials>>(prebranJsonString, jsonOptions);
            
            prebranUporabniki[0].FullName = "Novo Ime";
            prebranUporabniki[0].DarkMode = true;

            var posodobljeniJsonString = JsonSerializer.Serialize(prebranUporabniki, jsonOptions);
            File.WriteAllText(testJsonPath, posodobljeniJsonString);

            var koncniJsonString = File.ReadAllText(testJsonPath);
            var koncniUporabniki = JsonSerializer.Deserialize<List<UserCredentials>>(koncniJsonString, jsonOptions);

            Assert.AreEqual("Novo Ime", koncniUporabniki[0].FullName);
            Assert.IsTrue(koncniUporabniki[0].DarkMode);
        }
    }
}

