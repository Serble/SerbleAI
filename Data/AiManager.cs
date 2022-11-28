using System.Text;
using System.Text.Json;
using GeneralPurposeLib;

namespace SerbleAi.Data; 

public static class AiManager {
    public const string Personality = "A chatbot that is friendly\n\n";
    
    public static async Task<AiResponse> GetResponse(string token, string prompt, string? userid = null) {

        if (await ModerationCheck(token, prompt)) {
            return new AiResponse(true);
        }

        try {
            HttpClient client = new();
            client.BaseAddress = new Uri("https://api.openai.com/v1/completions");
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            string promptContent = prompt + "\nBot: ";

            StringContent content = userid == null
                ? new StringContent(
                    new {
                        model = "text-davinci-002",
                        prompt = promptContent,
                        temperature = 1,
                        max_tokens = 100,
                        top_p = 1,
                        frequency_penalty = 2,
                        presence_penalty = 2,
                    }.ToJson(), 
                    Encoding.UTF8, 
                    "application/json") : 
                new StringContent(
                    new {
                        model = "text-davinci-002",
                        prompt = promptContent,
                        temperature = 1,
                        max_tokens = 100,
                        top_p = 1,
                        frequency_penalty = 2,
                        presence_penalty = 2,
                        user = userid
                    }.ToJson(), 
                    Encoding.UTF8, 
                    "application/json");

            HttpResponseMessage response = await client.PostAsync(client.BaseAddress, content);
            string responseString = await response.Content.ReadAsStringAsync();
            JsonDocument document = JsonDocument.Parse(responseString);
            JsonElement root = document.RootElement;
            
            // Check for error
            if (root.TryGetProperty("error", out JsonElement error)) {
                string? code = error.GetProperty("code").GetString();
                string type = error.GetProperty("type").GetString()!;
                return code switch {
                    "invalid_api_key" => new AiResponse(0, "Invalid API key"),
                    _ => type switch {
                        "invalid_request_error" => new AiResponse(0, "Invalid API key"),
                        _ => throw new Exception("OpenAI API error: " + responseString)
                    }
                };
            }
            
            JsonElement choices = root.GetProperty("choices");
            JsonElement choice = choices[0];
            JsonElement text = choice.GetProperty("text");
        
            // token usage
            JsonElement usage = root.GetProperty("usage");
            JsonElement tokens = usage.GetProperty("total_tokens");
            return new AiResponse(tokens.GetInt32(), (text.GetString() ?? "").Replace("\n", ""));
        }
        catch (Exception e) {
            Logger.Error(e);
            return new AiResponse(0, "An error occured: " + e.Message);
        }
    }
    
    private static async Task<bool> ModerationCheck(string token, string context) {
        try {
            HttpClient client = new();
            client.BaseAddress = new Uri("https://api.openai.com/v1/moderations");
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            StringContent content = new(
                new {
                    input = context
                }.ToJson(),
                Encoding.UTF8, 
                "application/json");
            HttpResponseMessage response = await client.PostAsync(client.BaseAddress, content);
            string responseString = await response.Content.ReadAsStringAsync();
            Logger.Debug(responseString);
            JsonDocument document = JsonDocument.Parse(responseString);
            JsonElement root = document.RootElement;
            
            // Check for error
            if (root.TryGetProperty("error", out JsonElement _)) {
                return false;
            }
            
            JsonElement results = root.GetProperty("results");
            JsonElement result = results[results.GetArrayLength()-1];
            return result.GetProperty("flagged").GetBoolean();
        }
        catch (Exception e) {
            Logger.Error(e);
            return false;
        }
    }

}

public class AiResponse {
    public int UsedTokens { get; set; }
    public string Text { get; set; }
    public bool Flagged { get; set; }
    
    public AiResponse(int usedTokens, string text) {
        UsedTokens = usedTokens;
        Text = text;
    }

    public AiResponse(bool flagged) {
        Flagged = flagged;
        Text = "Message flagged by OpenAI";
        UsedTokens = 0;
    }

    public override string ToString() {
        return Text;
    }
}