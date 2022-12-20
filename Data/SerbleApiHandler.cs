using System.Net;
using System.Text;
using System.Text.Json;
using GeneralPurposeLib;
using SerbleAi.Data.Schemas;

namespace SerbleAi.Data;
public static class SerbleApiHandler {

    public static async Task<SerbleApiResponse<User>> GetUser(string token) {
        // Send HTTP request to API
        HttpClient client = new();
        client.DefaultRequestHeaders.Add("SerbleAuth", "App " + token);
        HttpResponseMessage response;
        try {
            response = await client.GetAsync(GlobalConfig.Config["SerbleApiUrl"] + "account");
        }
        catch (Exception e) {
            return new SerbleApiResponse<User>(false, "Failed: " + e);
        }
        if (!response.IsSuccessStatusCode) {
            return new SerbleApiResponse<User>(false, $"Failed: {response.StatusCode}");
        }
        // Parse response
        string json = await response.Content.ReadAsStringAsync();
        User user;
        try {
            user = JsonSerializer.Deserialize<User>(json).ThrowIfNull();
        }
        catch (Exception e) {
            return new SerbleApiResponse<User>(false, $"Failed to parse response: {e.Message}");
        }
        return new SerbleApiResponse<User>(user);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="oauthCode"></param>
    /// <returns>(Refresh Token, Access Token)</returns>
    public static async Task<SerbleApiResponse<(string, string)>> GetRefreshToken(string oauthCode) {
        // Send HTTP request to API
        HttpClient client = new();
        HttpResponseMessage response;
        try {
            response = await client.PostAsync(GlobalConfig.Config["SerbleApiUrl"] + 
                                              $"oauth/token/refresh?" +
                                              $"code={oauthCode}" +
                                              $"&client_id={GlobalConfig.Config["ClientID"]}" +
                                              $"&client_secret={GlobalConfig.Config["AppSecret"]}" +
                                              $"&grant_type=authorization_code", new StringContent(""));
        }
        catch (Exception e) {
            return new SerbleApiResponse<(string, string)>(false, "Error: " + e);
        }
        if (!response.IsSuccessStatusCode) {
            return new SerbleApiResponse<(string, string)>(false, $"Non Success Code: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");
        }
        // Parse response
        string refreshToken;
        string accessToken;
        try {
            JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            refreshToken = doc.RootElement.GetProperty("refresh_token").GetString()!;
            accessToken = doc.RootElement.GetProperty("access_token").GetString()!;
        }
        catch (Exception e) {
            return new SerbleApiResponse<(string, string)>(false, $"Failed to parse response: {e.Message}");
        }
        return new SerbleApiResponse<(string, string)>((refreshToken, accessToken));
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="refreshToken">The user's oauth refresh token</param>
    /// <returns>Access Token</returns>
    public static async Task<SerbleApiResponse<string>> GetAccessToken(string refreshToken) {
        // Send HTTP request to API
        HttpClient client = new();
        HttpResponseMessage response;
        try {
            response = await client.PostAsync(GlobalConfig.Config["SerbleApiUrl"] + 
                                              $"oauth/token/access?" +
                                              $"refresh_token={refreshToken}" +
                                              $"&client_id={GlobalConfig.Config["ClientID"]}" +
                                              $"&client_secret={GlobalConfig.Config["AppSecret"]}" +
                                              $"&grant_type=authorization_code", new StringContent(""));
        }
        catch (Exception e) {
            return new SerbleApiResponse<string>(false, "Error: " + e);
        }
        if (!response.IsSuccessStatusCode) {
            return new SerbleApiResponse<string>(false, $"Non Success Code: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");
        }
        // Parse response
        string accessToken;
        try {
            JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            accessToken = doc.RootElement.GetProperty("access_token").GetString()!;
        }
        catch (Exception e) {
            return new SerbleApiResponse<string>(false, $"Failed to parse response: {e.Message}");
        }
        return new SerbleApiResponse<string>(accessToken);
    }
    
    public static async Task<SerbleApiResponse<User>> RegisterUser(string username, string password, string recaptchaToken) {
        // Send HTTP request to API
        HttpClient client = new();
        client.DefaultRequestHeaders.Add("SerbleAntiSpam", $"recaptcha {recaptchaToken}");
        HttpResponseMessage response;
        try {
            response = await client.PostAsync(GlobalConfig.Config["SerbleApiUrl"] + "account", new StringContent(new {
                username,
                password
            }.ToJson(), Encoding.UTF8, "application/json"));
        }
        catch (Exception e) {
            return new SerbleApiResponse<User>(false, "Failed: " + e);
        }
        if (!response.IsSuccessStatusCode) {
            return new SerbleApiResponse<User>(false, $"Failed: {response.StatusCode} ({await response.Content.ReadAsStringAsync()})");
        }
        // Parse response
        string json = await response.Content.ReadAsStringAsync();
        User user;
        try {
            user = JsonSerializer.Deserialize<User>(json).ThrowIfNull();
        }
        catch (Exception e) {
            return new SerbleApiResponse<User>(false, $"Failed to parse response: {e.Message}");
        }
        return new SerbleApiResponse<User>(user);
    }
    
    public static async Task<SerbleApiResponse<User>> EditUser(string token, AccountEditRequest[] edits) {
        // Send HTTP request to API
        HttpClient client = new();
        client.DefaultRequestHeaders.Add("SerbleAuth", "User " + token);
        HttpResponseMessage response;
        string jsonInp = edits.ToJson();
        try {
            response = await client.PatchAsync(
                GlobalConfig.Config["SerbleApiUrl"] + "account", 
                new StringContent(jsonInp, Encoding.UTF8, 
                    "application/json"));
        }
        catch (Exception e) {
            return new SerbleApiResponse<User>(false, "Failed: " + e);
        }
        if (!response.IsSuccessStatusCode) {
            string flag = "unknown";
            string responseContent = await response.Content.ReadAsStringAsync();
            if (response.StatusCode == HttpStatusCode.BadRequest) {
                flag = responseContent switch {
                    "Username is already taken" => "name-taken",
                    "Invalid email" => "email-invalid",
                    "Field doesn't exist" => "bad-field",
                    _ => flag
                };
            }
            return new SerbleApiResponse<User>(false, $"Failed: {response.StatusCode} ({await response.Content.ReadAsStringAsync()})", flag);
        }
        // Parse response
        string json = await response.Content.ReadAsStringAsync();
        User user;
        try {
            user = JsonSerializer.Deserialize<User>(json).ThrowIfNull();
        }
        catch (Exception e) {
            return new SerbleApiResponse<User>(false, $"Failed to parse response: {e.Message}");
        }
        return new SerbleApiResponse<User>(user);
    }

    public static async Task<SerbleApiResponse<PublicOAuthApp>> GetPublicAppInfo(string appId) {
        // Send HTTP request to API
        HttpClient client = new();
        HttpResponseMessage response;
        try {
            response = await client.GetAsync($"{GlobalConfig.Config["SerbleApiUrl"]}app/{appId}/public");
        }
        catch (Exception e) {
            return new SerbleApiResponse<PublicOAuthApp>(false, "Failed: " + e);
        }
        if (!response.IsSuccessStatusCode) {
            string flag = "unknown";
            string responseContent = await response.Content.ReadAsStringAsync();
            if (response.StatusCode == HttpStatusCode.NotFound) {
                flag = "not-found";
            }
            return new SerbleApiResponse<PublicOAuthApp>(false, $"Failed: {response.StatusCode} ({await response.Content.ReadAsStringAsync()})", flag);
        }
        // Parse response
        string json = await response.Content.ReadAsStringAsync();
        PublicOAuthApp app;
        try {
            app = JsonSerializer.Deserialize<PublicOAuthApp>(json).ThrowIfNull();
        }
        catch (Exception e) {
            return new SerbleApiResponse<PublicOAuthApp>(false, $"Failed to parse response: {e.Message}");
        }
        return new SerbleApiResponse<PublicOAuthApp>(app);
    }
    
    public static async Task<SerbleApiResponse<string>> AuthorizeApp(string token, string appId, string scopeString) {
        // Send HTTP request to API
        HttpClient client = new();
        client.DefaultRequestHeaders.Add("SerbleAuth", "User " + token);
        HttpResponseMessage response;
        string jsonInp = new AuthorizedApp(appId, scopeString).ToJson();
        try {
            response = await client.PostAsync($"{GlobalConfig.Config["SerbleApiUrl"]}account/authorizedApps", 
                new StringContent(jsonInp, Encoding.UTF8, 
                "application/json"));
        }
        catch (Exception e) {
            return new SerbleApiResponse<string>(false, "Failed: " + e);
        }

        if (response.IsSuccessStatusCode)
            return new SerbleApiResponse<string>(await response.Content.ReadAsStringAsync());
        string flag = "unknown";
        string responseContent = await response.Content.ReadAsStringAsync();
        if (response.StatusCode == HttpStatusCode.BadRequest) {
            flag = "bad-app";
        }
        return new SerbleApiResponse<string>(false, $"Failed: {response.StatusCode} ({await response.Content.ReadAsStringAsync()})", flag);
    }
    
    public static async Task<SerbleApiResponse<double>> CheckReCaptcha(string token) {
        // Send HTTP request to API
        HttpClient client = new();
        HttpResponseMessage response;
        string url = $"{GlobalConfig.Config["SerbleApiUrl"]}recaptcha";
        url += "?token=" + token;
        try {
            response = await client.PostAsync(url, null);
        }
        catch (Exception e) {
            return new SerbleApiResponse<double>(false, "Failed: " + e);
        }
        if (!response.IsSuccessStatusCode) {
            return new SerbleApiResponse<double>(false, $"Failed: {response.StatusCode}");
        }
        // Parse response
        string responseStr = await response.Content.ReadAsStringAsync();
        return new SerbleApiResponse<double>(double.Parse(responseStr));
    }

    public static async Task<SerbleApiResponse<OAuthApp[]>> GetUsersApps(string token) {
        // Send HTTP request to API
        HttpClient client = new();
        HttpResponseMessage response;
        client.DefaultRequestHeaders.Add("SerbleAuth", "User " + token);
        try {
            response = await client.GetAsync($"{GlobalConfig.Config["SerbleApiUrl"]}app");
        }
        catch (Exception e) {
            return new SerbleApiResponse<OAuthApp[]>(false, "Failed: " + e);
        }
        if (!response.IsSuccessStatusCode) {
            return new SerbleApiResponse<OAuthApp[]>(false, $"Failed: {response.StatusCode}");
        }
        // Parse response
        string responseStr = await response.Content.ReadAsStringAsync();
        try {
            return new SerbleApiResponse<OAuthApp[]>(JsonSerializer.Deserialize<OAuthApp[]>(responseStr).ThrowIfNull());
        }
        catch (Exception e) {
            return new SerbleApiResponse<OAuthApp[]>(false, $"Failed to parse response: {e.Message}");
        }
    }

    public static async Task<SerbleApiResponse<bool>> CreateOAuthApp(string token, PublicOAuthApp app) {
        // Send HTTP request to API
        HttpClient client = new();
        HttpResponseMessage response;
        client.DefaultRequestHeaders.Add("SerbleAuth", "User " + token);
        string jsonInp = new { app.Name, app.Description }.ToJson();
        try {
            response = await client.PostAsync($"{GlobalConfig.Config["SerbleApiUrl"]}app", new StringContent(jsonInp, Encoding.UTF8, "application/json"));
        }
        catch (Exception e) {
            return new SerbleApiResponse<bool>(false, "Failed: " + e);
        }
        if (!response.IsSuccessStatusCode) {
            return new SerbleApiResponse<bool>(false, $"Failed: {response.StatusCode}");
        }
        // Don't parse response
        return new SerbleApiResponse<bool>(true);
    }

}

public class SerbleApiResponse<T> {
    
    public bool Success { get; }
    public T? ResponseObject { get; }
    public string ErrorMessage { get; }
    public string ErrorFlag { get; }
    
    public SerbleApiResponse(T responseObject) {
        ResponseObject = responseObject;
        Success = true;
        ErrorFlag = "";
        ErrorMessage = "";
    }
    
    public SerbleApiResponse(bool success, string errorMessage, string errorFlag = "") {
        Success = false;
        ResponseObject = default;
        ErrorMessage = errorMessage;
        ErrorFlag = errorFlag;
    }
    
    public static implicit operator T(SerbleApiResponse<T> response) {
        return response.ResponseObject.ThrowIfNull();
    }

}