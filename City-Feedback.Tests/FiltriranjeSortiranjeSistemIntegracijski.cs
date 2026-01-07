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
    public class FiltriranjeSortiranjeSistemIntegracijski
    {
        private string testJsonPath;
        private JsonSerializerOptions jsonOptions;

        [TestInitialize]
        public void Priprava()
        {
            testJsonPath = Path.Combine(Path.GetTempPath(), $"test_filtriranje_{Guid.NewGuid()}.json");
            
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
        public void FiltriranjePoCategoriji_Ceste_PravilnoFiltrirane()
        {
            var uporabniki = new List<UserCredentials>
            {
                new UserCredentials
                {
                    Id = 1,
                    Username = "testni",
                    Prijave = new List<Prijava>
                    {
                        new Prijava { Naslov = "Prijava 1", Kategorija = "Ceste" },
                        new Prijava { Naslov = "Prijava 2", Kategorija = "Odpadki" },
                        new Prijava { Naslov = "Prijava 3", Kategorija = "Ceste" }
                    }
                }
            };

            var jsonString = JsonSerializer.Serialize(uporabniki, jsonOptions);
            File.WriteAllText(testJsonPath, jsonString);

            var prebranJsonString = File.ReadAllText(testJsonPath);
            var prebranUporabniki = JsonSerializer.Deserialize<List<UserCredentials>>(prebranJsonString, jsonOptions);

            var vsePrijave = prebranUporabniki.SelectMany(u => u.Prijave).ToList();
            var filtriranePrijave = vsePrijave.Where(p => p.Kategorija == "Ceste").ToList();

            Assert.AreEqual(2, filtriranePrijave.Count);
        }

        [TestMethod]
        public void SortiranjePoDatumu_NajnovejsePrvic_PravilnoSortirano()
        {
            var datum1 = DateTime.Now.AddDays(-3);
            var datum2 = DateTime.Now.AddDays(-1);
            var datum3 = DateTime.Now;

            var uporabniki = new List<UserCredentials>
            {
                new UserCredentials
                {
                    Id = 1,
                    Username = "testni",
                    Prijave = new List<Prijava>
                    {
                        new Prijava { Naslov = "Stara prijava", Datum = datum1 },
                        new Prijava { Naslov = "Nova prijava", Datum = datum3 },
                        new Prijava { Naslov = "Srednja prijava", Datum = datum2 }
                    }
                }
            };

            var jsonString = JsonSerializer.Serialize(uporabniki, jsonOptions);
            File.WriteAllText(testJsonPath, jsonString);

            var prebranJsonString = File.ReadAllText(testJsonPath);
            var prebranUporabniki = JsonSerializer.Deserialize<List<UserCredentials>>(prebranJsonString, jsonOptions);

            var vsePrijave = prebranUporabniki.SelectMany(u => u.Prijave).ToList();
            var sortiranoPoNajnovejsih = vsePrijave.OrderByDescending(p => p.Datum).ToList();

            Assert.AreEqual("Nova prijava", sortiranoPoNajnovejsih[0].Naslov);
            Assert.AreEqual("Stara prijava", sortiranoPoNajnovejsih[2].Naslov);
        }
    }
}

