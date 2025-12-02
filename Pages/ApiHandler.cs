using System.Net;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
    public const string BaseUrl = "https://ilmeee.com/smart_inventory_solution/";
    public const string LoginUrl = BaseUrl + "accounts/";
    public const string RegisterUrl = BaseUrl + "register/";

    public async Task<InventoryResponse> getInfoInventory()
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer()
        };

        using var client = new HttpClient(handler);

        var response = await client.GetAsync($"{BaseUrl}");
        var jsonString = await response.Content.ReadAsStringAsync();

        // Convert JSON string ke object InventoryResponse
        var inventoryResponse = JsonConvert.DeserializeObject<InventoryResponse>(jsonString);
        return inventoryResponse;
    }

    public async Task<ApiResponse> Post(string url, Dictionary<string, string> payload)
    {
        var content = new FormUrlEncodedContent(payload);

        var handler = new HttpClientHandler { CookieContainer = new CookieContainer() };
        using var client = new HttpClient(handler);
        var response = await client.PostAsync(url, content);
        
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
#endregion
