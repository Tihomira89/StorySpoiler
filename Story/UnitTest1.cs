using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;







namespace Story
{
    [TestFixture]
    public class Story
    {
        private RestClient client;
        private static string createdStoryId;
        private static string createdFoodId;
        private object response;
        private object storyIdElement;
        private const string baseUrl = "https://d3s5nxhwblsjbi.cloudfront.net";

        public object StoryTests { get; private set; }

        [OneTimeSetUp]
        public void Setup()
        {
            string token = GetJwtToken("Ema3", "Ema123456");

            var options = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };

            client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            var logginClient = new RestClient(baseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);

            request.AddJsonBody(new { username, password });

            var response = logginClient.Execute(request);

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);

            return json.GetProperty("accessToken").GetString() ?? string.Empty;


        }

        

        [Test, Order(1)]
        public void CreateStorySpoiler_WithRequiredFields_ShouldReturnCreated()
        {
            // arrange
            var storyRequest = new
            {
                Title = "New Story Spoiler",
                Description = "Test story spoiler description",
                Url = ""
            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(storyRequest);

            // act
            var response = this.client.Execute(request);

            // Debug: print the response content
            Console.WriteLine("CreateStorySpoiler response: " + response.Content);

            // assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            var responseBody = JsonSerializer.Deserialize<JsonElement>(response.Content);

            // Assert that StoryId is returned
            Assert.That(responseBody.TryGetProperty("storyId", out var storyIdElement), Is.True,
                "Response should contain a storyId property");

            createdStoryId = storyIdElement.GetGuid().ToString();
            Assert.That(Guid.TryParse(createdStoryId, out var guid) && guid != Guid.Empty,
                "storyId should be a valid, non-empty GUID");

            // Assert that the response message indicates success
            Assert.That(responseBody.TryGetProperty("msg", out var msgElement), Is.True,
                "Response should contain a msg property");
            Assert.That(msgElement.GetString(), Is.EqualTo("Successfully created!"));
        }






        [Test, Order(2)]
        public void EditStorySpoiler_WithValidStoryId_ShouldReturnOk()
        {
            // arrange
            Assert.That(!string.IsNullOrEmpty(createdStoryId), "createdStoryId should not be null or empty");

            var editRequestBody = new
            {
                Title = "Edited Story Spoiler",
                Description = "Edited description",
                Url = ""
            };

            var request = new RestRequest($"/api/Story/Edit/{createdStoryId}", Method.Put);
            request.AddJsonBody(editRequestBody);

            // act
            var response = this.client.Execute(request);

            // Debug: print the response content
            Console.WriteLine("EditStorySpoiler response: " + response.Content);

            // assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var responseBody = JsonSerializer.Deserialize<JsonElement>(response.Content);

            // Assert that the response message indicates success
            Assert.That(responseBody.TryGetProperty("msg", out var msgElement), Is.True,
                "Response should contain a msg property");
            Assert.That(msgElement.GetString(), Is.EqualTo("Successfully edited"));
        }

        [Test, Order(3)]
        public void GetAllStorySpoilers_ShouldReturnOkAndNonEmptyArray()
        {
            // arrange
            var request = new RestRequest("/api/Story/All", Method.Get);

            // act
            var response = this.client.Execute(request);

            // Debug: print the response content
            Console.WriteLine($"Status: {response.StatusCode}, Content: {response.Content}");

            // assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Status code should be 200 OK");

            var responseBody = JsonSerializer.Deserialize<JsonElement>(response.Content);

            // Assert that the response is a non-empty array
            Assert.That(responseBody.ValueKind, Is.EqualTo(JsonValueKind.Array), "Response should be a JSON array");
            Assert.That(responseBody.GetArrayLength(), Is.GreaterThan(0), "Array of stories should not be empty");
        }

        [Test, Order(4)]
        public void DeleteStorySpoiler_WithValidStoryId_ShouldReturnOk()
        {
            // arrange
            Assert.That(!string.IsNullOrEmpty(createdStoryId), "createdStoryId should not be null or empty");

            var request = new RestRequest($"/api/Story/Delete/{createdStoryId}", Method.Delete);

            // act
            var response = this.client.Execute(request);

            // Debug: print the response content
            Console.WriteLine("DeleteStorySpoiler response: " + response.Content);

            // assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Status code should be 200 OK");

            var responseBody = JsonSerializer.Deserialize<JsonElement>(response.Content);

            // Assert that the response message indicates successful deletion
            Assert.That(responseBody.TryGetProperty("msg", out var msgElement), Is.True,
                "Response should contain a msg property");
            Assert.That(msgElement.GetString(), Is.EqualTo("Deleted successfully!"));
        }

        [Test, Order(5)]
        public void CreateStorySpoiler_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            // arrange: missing Title and Description
            var incompleteStoryRequest = new
            {
                Url = ""
            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(incompleteStoryRequest);

            // act
            var response = this.client.Execute(request);

            // Debug: print the response content
            Console.WriteLine("CreateStorySpoiler (missing required fields) response: " + response.Content);

            // assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Status code should be 400 BadRequest");
        }

        [Test, Order(6)]
        public void EditStorySpoiler_WithNonExistingStoryId_ShouldReturnNotFound()
        {
            // arrange
            var nonExistingStoryId = Guid.NewGuid().ToString(); // unlikely to exist
            var editRequestBody = new
            {
                Title = "Should Not Exist",
                Description = "Trying to edit a non-existing story",
                Url = ""
            };

            var request = new RestRequest($"/api/Story/Edit/{nonExistingStoryId}", Method.Put);
            request.AddJsonBody(editRequestBody);

            // act
            var response = this.client.Execute(request);

            // Debug: print the response content
            Console.WriteLine("EditStorySpoiler (non-existing) response: " + response.Content);

            // assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), "Status code should be 404 NotFound");

            var responseBody = JsonSerializer.Deserialize<JsonElement>(response.Content);

            // Assert that the response message indicates "No spoilers..."
            Assert.That(responseBody.TryGetProperty("msg", out var msgElement), Is.True,
                "Response should contain a msg property");
            Assert.That(msgElement.GetString(), Does.Contain("No spoilers"), "Message should indicate 'No spoilers...'");
        }

        [Test, Order(7)]
        public void DeleteStorySpoiler_WithNonExistingStoryId_ShouldReturnBadRequest()
        {
            // arrange
            var nonExistingStoryId = Guid.NewGuid().ToString(); // unlikely to exist
            var request = new RestRequest($"/api/Story/Delete/{nonExistingStoryId}", Method.Delete);

            // act
            var response = this.client.Execute(request);

            // Debug: print the response content
            Console.WriteLine("DeleteStorySpoiler (non-existing) response: " + response.Content);

            // assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Status code should be 400 BadRequest");

            var responseBody = JsonSerializer.Deserialize<JsonElement>(response.Content);

            // Assert that the response message indicates "Unable to delete this story spoiler!"
            Assert.That(responseBody.TryGetProperty("msg", out var msgElement), Is.True,
                "Response should contain a msg property");
            Assert.That(msgElement.GetString(), Is.EqualTo("Unable to delete this story spoiler!"));
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            client?.Dispose();
        }
    }
}
