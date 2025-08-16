using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;
using StorySpoiler.Models;


namespace StorySpoiler
{
    [TestFixture]
    public class StorySpoilerTests
    {
        private RestClient client;
        private static string createdStoryId;
        private const string baseUrl = "https://d3s5nxhwblsjbi.cloudfront.net";

        [OneTimeSetUp]
        public void Setup()
        {
            string token = GetJwtToken("inso92", "inso92");

            var options = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };

            client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            var loginClient = new RestClient(baseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);

            request.AddJsonBody(new { username, password });

            var response = loginClient.Execute(request);

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);

            return json.GetProperty("accessToken").GetString() ?? string.Empty;

        }

        [Test, Order(1)]
        public void CreateStory_ShouldReturnCreated()
        {
                var story = new
                {
                    Title = "New Story",
                    Description = "Interesting story",
                    Url = ""
                };

                var request = new RestRequest("/api/Story/Create", Method.Post);
                request.AddJsonBody(story);

                var response = client.Execute(request);

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

                var json = JsonSerializer.Deserialize<JsonElement>(response.Content);

                createdStoryId = json.GetProperty("storyId").GetString() ?? string.Empty;

                Assert.That(createdStoryId, Is.Not.Null.And.Not.Empty, "Story ID should not be null or empty.");
                Assert.That(response.Content, Does.Contain("Successfully created!"));

        }

        [Test, Order(2)]
        public void EditStory_ShouldReturnSuccess()
        {
            var changes = new StoryDTO
            {
                Title = "Edited Story",
                Description = "This is an updated test story description.",
                Url = ""
            };

            var request = new RestRequest($"/api/Story/Edit/{createdStoryId}", Method.Put);

            request.AddJsonBody(changes);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo((HttpStatusCode)HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("Successfully edited"));
        }

        [Test, Order(3)]
        public void GetAllStories_ReturnList()
        {
            var request = new RestRequest("api/Story/All", Method.Get);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo((HttpStatusCode)HttpStatusCode.OK));

            var story = JsonSerializer.Deserialize<List<object>>(response.Content);
            Assert.That(story, Is.Not.Empty);
        }

        [Test, Order(4)]
        public void DeleteStory_ReturnSuccess()
        {
            var request = new RestRequest($"/api/Story/Delete/{createdStoryId}", Method.Delete);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("Deleted successfully!"));
        }

        [Test, Order(5)]
        public void CreateStory_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            var story = new
            {
                Title = "",
                Description = ""
            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(story);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo((HttpStatusCode)HttpStatusCode.BadRequest));
        }

        [Test, Order(6)]
        public void EditNonExistingStory_ShouldReturnNotFound()
        {
            string fakeID = "123";

            var changes = new StoryDTO
            {
                Title = "Edited Non-Existing Story",
                Description = "This is an updated test story description for a non-existing story.",
                Url = ""
            };

            var request = new RestRequest($"/api/Story/Edit/{fakeID}", Method.Put);
            request.AddJsonBody(changes);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(response.Content, Does.Contain("No spoilers..."));
        }

        [Test, Order(7)]
        public void DeleteNonExistingStory_ShouldReturnBadRequest()
        {
            string fakeID = "123";

            var request = new RestRequest($"/api/Story/Delete/{fakeID}", Method.Delete);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("Unable to delete this story spoiler!"));
        }



        [OneTimeTearDown]
        public void Cleanup()
        {
            client?.Dispose();
        }
    }
}