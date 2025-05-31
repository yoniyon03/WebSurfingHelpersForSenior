using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WebSurfingHelpersForSenior
{
    public class GeminiService
    {
        private readonly string apiKey = "AIzaSyBgv-ZfAqxf4sSHPCCalxzIfSZIFsogGRM";
        private readonly HttpClient client = new HttpClient();

        public async Task<string> GetRecommendation(string prompt)
        {
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            string json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                string url = $"https://generativelanguage.googleapis.com/v1beta1/models/gemini-pro:generateContent?key={apiKey}";

                var response = await client.PostAsync(url, content);
                string responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return $"서버 응답 오류: {(int)response.StatusCode} {response.ReasonPhrase}\n내용: {responseText}";
                }

                using (var doc = JsonDocument.Parse(responseText))
                {
                    var root = doc.RootElement;

                    if (root.TryGetProperty("candidates", out JsonElement candidates) &&
                        candidates.GetArrayLength() > 0)
                    {
                        var contentEl = candidates[0].GetProperty("content");
                        var part = contentEl.GetProperty("parts")[0];
                        return part.GetProperty("text").GetString();
                    }

                    return "AI 응답에서 추천 내용을 찾지 못했습니다.";
                }
            }
            catch (HttpRequestException ex)
            {
                return "요청 실패: " + ex.Message;
            }
            catch (JsonException ex)
            {
                return "JSON 파싱 오류: " + ex.Message;
            }
        }
    }
}
