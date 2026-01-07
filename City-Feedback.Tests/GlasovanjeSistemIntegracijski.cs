using Microsoft.VisualStudio.TestTools.UnitTesting;
using City_Feedback.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace City_Feedback.Tests
{
    [TestClass]
    public class GlasovanjeSistemIntegracijski
    {
        private string testJsonPath;
        private JsonSerializerOptions jsonOptions;

        [TestInitialize]
        public void Priprava()
        {
            testJsonPath = Path.Combine(Path.GetTempPath(), $"test_glasovanje_{Guid.NewGuid()}.json");
            
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
        public void DodajUpvote_UporabnikGlasuje_ShranjenaVJson()
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
                            Upvotes = 0,
                            UpvotedBy = new List<string>()
                        }
                    }
                }
            };

            var jsonString = JsonSerializer.Serialize(uporabniki, jsonOptions);
            File.WriteAllText(testJsonPath, jsonString);

            var prebranJsonString = File.ReadAllText(testJsonPath);
            var prebranUporabniki = JsonSerializer.Deserialize<List<UserCredentials>>(prebranJsonString, jsonOptions);

            var glasovalec = "janez";
            prebranUporabniki[0].Prijave[0].UpvotedBy.Add(glasovalec);
            prebranUporabniki[0].Prijave[0].Upvotes++;

            var posodobljeniJsonString = JsonSerializer.Serialize(prebranUporabniki, jsonOptions);
            File.WriteAllText(testJsonPath, posodobljeniJsonString);

            var koncniJsonString = File.ReadAllText(testJsonPath);
            var koncniUporabniki = JsonSerializer.Deserialize<List<UserCredentials>>(koncniJsonString, jsonOptions);

            Assert.AreEqual(1, koncniUporabniki[0].Prijave[0].Upvotes);
            Assert.IsTrue(koncniUporabniki[0].Prijave[0].UpvotedBy.Contains(glasovalec));
        }

        [TestMethod]
        public void SpremeniDownvoteVUpvote_PreklopGlasovanja_ShranjenaVJson()
        {
            var glasovalec = "peter";
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
                            Upvotes = 0,
                            Downvotes = 1,
                            UpvotedBy = new List<string>(),
                            DownvotedBy = new List<string> { glasovalec }
                        }
                    }
                }
            };

            var jsonString = JsonSerializer.Serialize(uporabniki, jsonOptions);
            File.WriteAllText(testJsonPath, jsonString);

            var prebranJsonString = File.ReadAllText(testJsonPath);
            var prebranUporabniki = JsonSerializer.Deserialize<List<UserCredentials>>(prebranJsonString, jsonOptions);

            prebranUporabniki[0].Prijave[0].DownvotedBy.Remove(glasovalec);
            prebranUporabniki[0].Prijave[0].Downvotes--;
            prebranUporabniki[0].Prijave[0].UpvotedBy.Add(glasovalec);
            prebranUporabniki[0].Prijave[0].Upvotes++;

            var posodobljeniJsonString = JsonSerializer.Serialize(prebranUporabniki, jsonOptions);
            File.WriteAllText(testJsonPath, posodobljeniJsonString);

            var koncniJsonString = File.ReadAllText(testJsonPath);
            var koncniUporabniki = JsonSerializer.Deserialize<List<UserCredentials>>(koncniJsonString, jsonOptions);

            Assert.AreEqual(1, koncniUporabniki[0].Prijave[0].Upvotes);
            Assert.AreEqual(0, koncniUporabniki[0].Prijave[0].Downvotes);
            Assert.IsTrue(koncniUporabniki[0].Prijave[0].UpvotedBy.Contains(glasovalec));
        }
    }
}

