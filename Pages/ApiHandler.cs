using System.Net;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;
using dotenv.net;
using InventorySystem.GlobalVariables;
using InventorySystem.Controls;
using Newtonsoft.Json.Serialization;

namespace InventorySystem.Pages{
public class ApiResponse
{
    public HttpResponseMessage Response { get; }
    public CookieContainer Cookies { get; }

    public ApiResponse(HttpResponseMessage response, CookieContainer cookies)
    {
        Response = response;
        Cookies = cookies;
    }
}

public class ApiHandler
{
    public const string BaseUrl = "https://ilmeee.com/smart_inventory_solution";
    public const string LoginUrl = BaseUrl + "/accounts/";
    public const string RegisterUrl = BaseUrl + "/register/";
    public const string getAllInformDatabase = BaseUrl + "/get_allinform_database/";
    public const string HandlerRequest = BaseUrl + "/handler_request/";
    public List<string> allModels =  new List<string>{ "gemma-3-27b-it", "gemini-2.5-flash", "gemini-2.0-flash-lite", "gemini-2.0-pro" };
    public const string GeminiUrl = "https://generativelanguage.googleapis.com/v1beta/models/";
    private const string geminiApiKey = "";
    private static readonly HttpClient client = new HttpClient();
    private int modelIndex = 0;

    public ApiHandler()
    {
        CheckApiKey();
    }

    public void CheckApiKey()
    {
        if (string.IsNullOrEmpty(geminiApiKey))
        {
            throw new InvalidOperationException("GEMINI_API_KEY is not set in environment variables.");   
        }
    }

    public async Task<InventoryResponse> getInfoInventory(string url)
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer()
        };

        using var client = new HttpClient(handler);

        var response = await client.GetAsync($"{url}");
        var jsonString = await response.Content.ReadAsStringAsync();

        // Convert JSON string ke object InventoryResponse
        var inventoryResponse = JsonConvert.DeserializeObject<InventoryResponse>(jsonString);
        return inventoryResponse;
    }

    public async Task<ChatHistoryResponse> getInfoHistory(Dictionary<string, string> payload)
    {
        var content = new FormUrlEncodedContent(payload);

        var handler = new HttpClientHandler { CookieContainer = new CookieContainer() };
        using var client = new HttpClient(handler);
        var response = await client.PostAsync(getAllInformDatabase, content);
        var jsonString = await response.Content.ReadAsStringAsync();

        // await SnackBar.Show($"Reponse: {jsonString}");
        // Convert JSON string ke object InventoryResponse
        var chatHistoryResponse = JsonConvert.DeserializeObject<ChatHistoryResponse>(jsonString);
        return chatHistoryResponse;
    }

    public async Task<ApiResponse> Post(string url, Dictionary<string, string> payload)
    {
        var content = new FormUrlEncodedContent(payload);

        var handler = new HttpClientHandler { CookieContainer = new CookieContainer() };
        using var client = new HttpClient(handler);
        var response = await client.PostAsync(url, content);
        
        return new ApiResponse(response, handler.CookieContainer);
    }

    public async Task<GeminiResponse> PostGemini(GeminiRequest payload)
    {
        // Menggunakan camelCase untuk properti JSON sesuai konvensi API
        string currentModel = allModels[modelIndex];
        var serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        var jsonPayload = JsonConvert.SerializeObject(payload, serializerSettings);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        var requestUri = $"{GeminiUrl}{currentModel}:generateContent?key={geminiApiKey}";

        var response = await client.PostAsync(requestUri, content);


        // 4. Tambahkan penanganan error yang kuat.
        // Periksa jika request tidak berhasil (misalnya, status code 4xx atau 5xx).
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            if (modelIndex >= allModels.Count())
            {
                modelIndex = 0;   
            }
            modelIndex++;
            throw new HttpRequestException($"Request API gagal dengan status code {response.StatusCode}: {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var geminiResponse = JsonConvert.DeserializeObject<GeminiResponse>(responseContent);

        if (geminiResponse == null)
        {
            throw new InvalidOperationException("Gagal melakukan deserialisasi respons dari GeminiAPI.");
        }
    
        return geminiResponse;   
    }

    public async Task<ApiResponse> Put(string url, Dictionary<string, string> payload)
    {
        var content = new FormUrlEncodedContent(payload);

        var handler = new HttpClientHandler { CookieContainer = new CookieContainer() };
        using var client = new HttpClient(handler);
        var response = await client.PutAsync(url, content);
        
        return new ApiResponse(response, handler.CookieContainer);
    }

    public async Task<ApiResponse> Delete(string url)
    {
        var handler = new HttpClientHandler { CookieContainer = new CookieContainer() };
        using var client = new HttpClient(handler);
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        
        var response = await client.DeleteAsync(url, cancellationToken);

        return new ApiResponse(response, handler.CookieContainer);
    }

    public async Task<string> readCookiesFile()
    {
        try
        {
            string appDataPath = FileSystem.AppDataDirectory;
            string filePath = Path.Combine(appDataPath, "cookies.txt");

            if (File.Exists(filePath))
            {
                using var reader = new StreamReader(filePath);
                string content = await reader.ReadToEndAsync();
                return content;
            }
            else
            {
                return null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error reading cookies file: {ex.Message}");
            return null;
        }
    }

    public async Task saveCookiesFile(string cookies)
    {
        string appDataPath = FileSystem.AppDataDirectory;
        string filePath = Path.Combine(appDataPath, "cookies.txt");

        using var writer = new StreamWriter(filePath, false);
        await writer.WriteAsync(cookies);
    }

    public async Task saveUserName(string username)
    {
        string appDataPath = FileSystem.AppDataDirectory;
        string filePath = Path.Combine(appDataPath, "username.txt");

        using var writer = new StreamWriter(filePath, false);
        await writer.WriteAsync(username);
    }

    public async Task<string> readUserName()
    {
        try
        {
            string appDataPath = FileSystem.AppDataDirectory;
            string filePath = Path.Combine(appDataPath, "username.txt");

            if (File.Exists(filePath))
            {
                using var reader = new StreamReader(filePath);
                string content = await reader.ReadToEndAsync();
                return content;
            }
            else
            {
                return null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error reading username file: {ex.Message}");
            return null;
        }
    }
 
    public async Task clearCookiesFile()
    {
        string appDataPath = FileSystem.AppDataDirectory;
        string filePath = Path.Combine(appDataPath, "cookies.txt");

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}

#region JSON Handler
public class InventoryResponse
{
    public string status { get; set; }
    public string message { get; set; }
    public List<InventoryData>? data { get; set; }
}

public class InventoryData
{
    public string? ID { get; set; }
    public string? created_at { get; set; }
    public string? name { get; set; }
    public string? stock { get; set; }
    public string? price { get; set; }
    public string? created_by { get; set; }
}

public class GeminiResponse
{
    public List<Candidate>? candidates { get; set; }
    public UsageMetadata? usageMetadata { get; set; }
}

public class Candidate
{
    public Content? content { get; set; }
    public string? finishReason { get; set; }
    public int index { get; set; }
}
#endregion
}