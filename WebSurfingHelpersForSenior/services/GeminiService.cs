using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SeniorWebHelper
{
    public class GeminiService
    {
        private readonly HttpClient _http = new HttpClient();
        private readonly string _apiKey = "AIzaSyBgv-ZfAqxf4sSHPCCalxzIfSZIFsogGRM";

        public async Task<string> SummarizeAsync(string inputText)
        {
            var json = JsonSerializer.Serialize(new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = inputText } } }
                }
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync($"https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key={_apiKey}", content);

            string result = await response.Content.ReadAsStringAsync();
            using (var doc = JsonDocument.Parse(result))
            {
                string summary = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return summary ?? "(요약 실패)";
            }
        }
    }
}
