using System.Net;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

public class ApiHandler
{
    public string baseUrl = "https://ilmeee.com/smart_inventory_solution/";

    public async Task<InventoryResponse> getInfoInventory()
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer()
        };

        using var client = new HttpClient(handler);

        var response = await client.GetAsync($"{baseUrl}");
        var jsonString = await response.Content.ReadAsStringAsync();

        // Convert JSON string ke object InventoryResponse
        var inventoryResponse = JsonConvert.DeserializeObject<InventoryResponse>(jsonString);
        return inventoryResponse;
    }

    public async Task<UserResponse> getInfoUser(string user)
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer()
        };

        using var client = new HttpClient(handler);

        var response = await client.GetAsync($"{baseUrl}?type=informaccount&email={user}");
        var jsonString = await response.Content.ReadAsStringAsync();

        // Convert JSON string ke object UserResponse
        var userResponse = JsonConvert.DeserializeObject<UserResponse>(jsonString);
        return userResponse;
    }


    public async Task<UserResponse?> getInfoUserSiswa(string user)
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer()
        };

        using var client = new HttpClient(handler);

        var response = client.GetAsync($"{baseUrl}?issiswa=true&email={user}");
        var jsonString = await response.Result.Content.ReadAsStringAsync();

        // Convert JSON string ke object UserResponse
        var userResponse = JsonConvert.DeserializeObject<UserResponse>(jsonString);
        return userResponse;
    }

    public async Task<string> responseGet(string baseUrl, string user)
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer()
        };

        using var client = new HttpClient(handler);
        
        var response = client.GetAsync($"{baseUrl}{user}");
        var jsonString = await response.Result.Content.ReadAsStringAsync();

        return jsonString;
    }

    public async Task<string> readCookiesFile()
    {
        try
        {
            // Path ke folder data aplikasi
            string appDataPath = FileSystem.AppDataDirectory;
            string filePath = Path.Combine(appDataPath, "cookies.txt");

            if (File.Exists(filePath))
            {
                // Baca isi file secara async
                using var reader = new StreamReader(filePath);
                string content = await reader.ReadToEndAsync();
                return content;
            }
            else
            {
                // Kalau file tidak ada, return string kosong/null
                return null;
            }
        }
        catch (Exception ex)
        {
            // Bisa log error atau tampilkan alert
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

}

#region JSON Handler
public class UserResponse
{
    public string status { get; set; }
    public string message { get; set; }
    public UserData? data { get; set; }
}

public class UserData
{
    public string? id { get; set; }
    public string? nama { get; set; }
    public string? guru_mapel { get; set; }
    public string? wali_kelas { get; set; }
    public string? foto { get; set; }
    public string? ttl { get; set; }
    public string? alamat { get; set; }
    public string? no_telp { get; set; }
    public string? email { get; set; }
    public string? password { get; set; }
    public string? admin { get; set; }
    public int? NISN { get; set; }
    public string? kelas { get; set; }

}

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
