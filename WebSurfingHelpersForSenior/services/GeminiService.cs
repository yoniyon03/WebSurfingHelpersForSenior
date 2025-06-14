using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;


public class GeminiService
{
    private readonly string apiKey = "AIzaSyBgv-ZfAqxf4sSHPCCalxzIfSZIFsogGRM";
    private readonly HttpClient httpClient = new HttpClient();





    public async Task<string> AskWhatIsThisPage(string currentUrl)
    { 
        var endpoint = $"https://generativelanguage.googleapis.com/v1/models/gemini-1.5-flash:generateContent?key={apiKey}";

        var prompt = $"이 웹사이트 주소는 {currentUrl}입니다. 이 사이트가 어떤 용도인지, 어떤걸 클릭하면 어떤걸 볼 수 있는지 시니어가 쉽게 이해할 수 있도록 설명해줘. 긴 문단 말고, 1~2문장마다 한 줄씩 끊어서 보기 쉽게, 가독성 좋게 답변해줘.";

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

        var requestJson = JsonSerializer.Serialize(requestBody);

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(endpoint),
            Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
        };

        HttpResponseMessage response = null;
        string responseContent = "";

        try
        {
            response = await httpClient.SendAsync(request);
            responseContent = await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            return $"[요청 자체 실패] {ex.Message}";
        }

        Debug.WriteLine($"🔍 상태코드: {response.StatusCode}");
        Debug.WriteLine($"📩 응답내용: {responseContent}");

        if (!response.IsSuccessStatusCode)
        {
            return $"[에러] 응답 실패: {response.StatusCode} / 내용: {responseContent}";
        }

        try
        {
            using (var jsonDoc = JsonDocument.Parse(responseContent))
            {
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("error", out var error))
                {
                    return $"[AI 응답 오류] {error.GetProperty("message").GetString()}";
                }

                return root
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();
            }
        }
        catch (Exception ex)
        {
            return $"[JSON 파싱 실패] {ex.Message}\n원본 응답:\n{responseContent}";
        }



    }


    public async Task<string> SummarizePageText(string pageText)
    {
        var endpoint = $"https://generativelanguage.googleapis.com/v1/models/gemini-1.5-flash:generateContent?key={apiKey}";

        var prompt = $"다음은 웹페이지의 텍스트 내용이야:\n\"\"\"\n{pageText}\n\"\"\"\n\n이 내용을 시니어가 쉽게 이해할 수 있도록 3줄 이내로 요약해줘. 긴 문단 말고, 1~2문장마다 한 줄씩 끊어서 보기 쉽게, 가독성 좋게 답변해줘. 각 줄은 **반드시 줄 바꿈**으로 구분해주세요.";

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

        var requestJson = JsonSerializer.Serialize(requestBody);

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(endpoint),
            Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
        };

        var response = await httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        Debug.WriteLine($"🔍 요약 응답 상태: {response.StatusCode}");
        Debug.WriteLine($"📩 요약 응답 내용: {responseContent}");

        if (!response.IsSuccessStatusCode)
        {
            return $"[에러] 요약 실패: {response.StatusCode} / 내용: {responseContent}";
        }

        try
        {
            using (var jsonDoc = JsonDocument.Parse(responseContent))
            {
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("error", out var error))
                {
                    return $"[요약 실패] {error.GetProperty("message").GetString()}";
                }

                return root
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();
            }
        }
        catch (Exception ex)
        {
            return $"[JSON 파싱 오류] {ex.Message}\n원본:\n{responseContent}";
        }
    }

    public async Task<string> ExplainWordMeaning(string word)
    {
        var endpoint = $"https://generativelanguage.googleapis.com/v1/models/gemini-1.5-flash:generateContent?key={apiKey}";

        var prompt = $@"
'{word}'이라는 단어의 뜻을 설명해줘. 
만약 외국어라면 번역하고 쉬운 뜻을 알려주고, 단어의 의미를 시니어가 잘 이해할 수 있게 설명해줘.
긴 문단 말고, 1~2문장마다 한 줄씩 끊어서 보기 쉽게, 가독성 좋게 답변해줘.";

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

        var requestJson = JsonSerializer.Serialize(requestBody);

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(endpoint),
            Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
        };

        var response = await httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return $"[에러] 단어 설명 실패: {response.StatusCode} / 내용: {responseContent}";
        }

        try
        {
            using (var jsonDoc = JsonDocument.Parse(responseContent))
            {
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("error", out var error))
                {
                    return $"[단어 설명 실패] {error.GetProperty("message").GetString()}";
                }

                return root
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

            }
        }
        catch (Exception ex)
        {
            return $"[JSON 파싱 오류] {ex.Message}\n내용:\n{responseContent}";
        }
    }

    public async Task<string> AnswerForUserQuery(string query)
    {
        var endpoint = $"https://generativelanguage.googleapis.com/v1/models/gemini-1.5-flash:generateContent?key={apiKey}";

        var prompt = $@"
{query}

위 문장의 의도를 파악해서, 시니어가 이해하기 쉽고 성의있게 답변을 정리해줘.
가능하다면 추천이나 구체적인 예시도 함께 주면 좋아.
긴 문단 말고, 1~2문장마다 한 줄씩 끊어서 보기 좋게 답변해줘.";

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

        var requestJson = JsonSerializer.Serialize(requestBody);

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(endpoint),
            Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
        };

        var response = await httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return $"[에러] AI 응답 실패: {response.StatusCode} / 내용: {responseContent}";
        }

        try
        {
            using (var jsonDoc = JsonDocument.Parse(responseContent))
            {
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("error", out var error))
                {
                    return $"[AI 응답 실패] {error.GetProperty("message").GetString()}";
                }

                return root
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();
            }
        }
        catch (Exception ex)
        {
            return $"[JSON 파싱 오류] {ex.Message}\n내용:\n{responseContent}";
        }
    }



    // 키워드로 웹서핑 유도
    public async Task<List<string>> RecommendKeywordsFromPageText(string pageText)
    {
        var endpoint = $"https://generativelanguage.googleapis.com/v1/models/gemini-1.5-flash:generateContent?key={apiKey}";

        var prompt = $@"
아래 본문을 읽고, 시니어들이 흥미를 가질 만한 관련 키워드를 3~5개 추천해줘.
각 키워드는 1~3단어 이내로 간단하게 출력해줘. 예시는 제외하고 키워드만 줄 바꿔서 출력해줘.

본문:
{pageText}
";

        var requestBody = new
        {
            contents = new[] {
            new {
                parts = new[] {
                    new { text = prompt }
                }
            }
        }
        };

        string requestJson = JsonSerializer.Serialize(requestBody);

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(endpoint),
            Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
        };

        HttpResponseMessage response;
        string json;
        try
        {
            response = await httpClient.SendAsync(request);
            json = await response.Content.ReadAsStringAsync();
            Debug.WriteLine("✅ Gemini 응답 원문:\n" + json);  // ✅ Gemini 응답 로그 출력
        }
        catch (Exception ex)
        {
            throw new Exception("Gemini 요청 실패: " + ex.Message);
        }

        List<string> keywords = new List<string>();
        JsonDocument doc = null;

        try
        {
            doc = JsonDocument.Parse(json);

            // 에러 응답 처리
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                var message = error.TryGetProperty("message", out var msg) ? msg.GetString() : "알 수 없는 오류";
                throw new Exception("Gemini 오류: " + message);
            }

            // 텍스트 추출
            string rawText = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            Debug.WriteLine("✅ Gemini가 반환한 키워드 원문:\n" + rawText);

            using (var reader = new StringReader(rawText))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string trimmed = line.Trim(); // 더 이상 '-' 안 자름
                    if (!string.IsNullOrWhiteSpace(trimmed))
                        keywords.Add(trimmed);
                }
            }

            return keywords;
        }
        catch (Exception ex)
        {
            throw new Exception("Gemini 응답 파싱 실패: " + ex.Message + "\n원본문:\n" + json);
        }
        finally
        {
            doc?.Dispose();
        }
    }





    public async Task<string> GetRecommendation(string prompt)
    {
        var requestBody = new
        {
            contents = new[] {
            new {
                parts = new[] {
                    new { text = prompt }
                }
            }
        }
        };

        string requestJson = JsonSerializer.Serialize(requestBody);
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"https://generativelanguage.googleapis.com/v1/models/gemini-1.5-flash:generateContent?key={apiKey}"),
            Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
        };

        try
        {
            var response = await httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            using (var doc = JsonDocument.Parse(json))
            {
                // 에러 응답 처리
                if (doc.RootElement.TryGetProperty("error", out var error))
                {
                    var msg = error.TryGetProperty("message", out var m) ? m.GetString() : "알 수 없는 오류";
                    return "[Gemini 오류] " + msg;
                }

                return doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();
            }
        }
        catch (Exception ex)
        {
            return "[추가 설명 실패] " + ex.Message;
        }
    }



    public async Task<string> GenerateSuggestionFromText(string pageText)
    {
        var endpoint = "https://generativelanguage.googleapis.com/v1/models/gemini-1.5-flash:generateContent?key=" + apiKey;

        string prompt = "다음은 사용자가 보고 있는 웹페이지의 본문입니다:\n\"\"\"\n"
     + pageText + "\n\"\"\"\n\n"
     + "이 페이지에서 시니어가 궁금해할 수 있는 구체적인 주제를 한 문장으로 질문형으로 제안해줘.\n"
     + "예: '주민등록등본 발급 방법이 궁금하신가요?', '오늘 날씨를 알려드릴까요?'\n"
     + "너가 대답해주기 어려운 주제는 제외하고 너가 잘 알려줄 수 있는 제안을 해줘.";


        var requestBody = new
        {
            contents = new[]
            {
            new
            {
                parts = new[] { new { text = prompt } }
            }
        }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };

        var response = await httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        JsonDocument doc = null;
        try
        {
            doc = JsonDocument.Parse(json);
            return doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();
        }
        finally
        {
            if (doc != null) doc.Dispose();
        }
    }

    public async Task<string> ExplainTopicStepByStep(string topic)
    {
        var endpoint = "https://generativelanguage.googleapis.com/v1/models/gemini-1.5-flash:generateContent?key=" + apiKey;

        string prompt = "'" + topic + "'에 대해 시니어가 따라하기 쉽게 단계별로 설명해주거나 단계별로 따라하는 주제가 아니라면 시니어가 이해하기 쉽게 설명해줘. PC기준으로 설명해줘야 해.\n"
            + "만약 단계별로 설명하게 된다면 각 단계는 한 줄씩, 간단하고 구체적인 지시문으로 구성해줘. 긴 문단 말고, 1~2문장마다 한 줄씩 끊어서 보기 쉽게, 가독성 좋게 답변해줘. 가족한테 부탁하라거나 주변 사람들한테 부탁하라는 말은 하면 안돼..!!";

        var requestBody = new
        {
            contents = new[]
            {
            new
            {
                parts = new[] { new { text = prompt } }
            }
        }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };

        var response = await httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        JsonDocument doc = null;
        try
        {
            doc = JsonDocument.Parse(json);
            return doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();
        }
        finally
        {
            if (doc != null) doc.Dispose();
        }
    }


}


