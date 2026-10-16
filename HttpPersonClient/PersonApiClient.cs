using System.Net;
using System.Text;
using System.Text.Json;

namespace HttpPersonClient
{

    public class PersonApiClient
    {
        private readonly HttpClient httpClient;
        private readonly string baseUrl;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public PersonApiClient(string baseUrl)
        {
            if (!baseUrl.EndsWith("/")) baseUrl += "/";
            this.baseUrl = baseUrl;

            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            httpClient = new HttpClient(handler);
            httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<List<Person>> GetAllAsync()
        {
            string url = baseUrl + "api/persons";
            var response = await httpClient.GetAsync(url);
            string body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"HTTP {(int)response.StatusCode}: {body}");

            return JsonSerializer.Deserialize<List<Person>>(body, JsonOptions)
                   ?? new List<Person>();
        }

        public async Task<Person> GetByIdAsync(string id)
        {
            string url = baseUrl + "api/persons/" + id;
            var response = await httpClient.GetAsync(url);
            string body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"HTTP {(int)response.StatusCode}: {body}");

            return JsonSerializer.Deserialize<Person>(body, JsonOptions);
        }

        public async Task<Person> CreateAsync(Person person)
        {
            string url = baseUrl + "api/persons";
            string json = JsonSerializer.Serialize(person, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(url, content);
            string body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"HTTP {(int)response.StatusCode}: {body}");

            return JsonSerializer.Deserialize<Person>(body, JsonOptions);
        }

        public async Task<Person> UpdateAsync(string id, Person person)
        {
            string url = baseUrl + "api/persons/" + id;
            string json = JsonSerializer.Serialize(person, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PutAsync(url, content);
            string body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"HTTP {(int)response.StatusCode}: {body}");

            return JsonSerializer.Deserialize<Person>(body, JsonOptions);
        }

        public async Task DeleteAsync(string id)
        {
            string url = baseUrl + "api/persons/" + id;
            var response = await httpClient.DeleteAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync();
                throw new Exception($"HTTP {(int)response.StatusCode}: {body}");
            }
        }
    }
}
