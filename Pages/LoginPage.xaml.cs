using Microsoft.Maui.Controls;
using System;
using System.Text;
using System.Net.Http;
using System.Net;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
// using BCrypt;

namespace InventorySystem.Pages;

public partial class LoginPage : ContentPage
{
    ApiHandler handlerApi = new ApiHandler();
    public LoginPage()
    {
        InitializeComponent();
        // RequestCameraPermission();
        RequestLocationPermission();
        RequestCameraPermission();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        // Disable button to prevent multiple clicks
        LoginButton.IsEnabled = false;
        LoginButton.Text = "Signing in...";

        try
        {
			// Get input values
			// string idKontrak = IDKontrak.Text?.Trim();
            string email = EmailEntry.Text?.Trim();
            string password = PasswordEntry.Text;
            bool rememberMe = RememberMeCheckBox.IsChecked;

            // Basic validation
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlertAsync("Error", "Please enter your email address.", "OK");
                return;
            }

            // Simulate login process
            await Task.Delay(2000); // Simulate network delay
			string sessionValue = await LoginAndGetSessionAsync(email, password);
            // save cookies
            await handlerApi.saveCookiesFile(sessionValue);
            await DisplayAlertAsync("Login Status", sessionValue, "OK");
            // For demo purposes - you would integrate with your actual authentication service
            // if (email.ToLower() == "demo@example.com" && password == "demo123")
            // {
            // 	await DisplayAlertAsync("Success", $"Welcome back!\nRemember me: {rememberMe}", "OK");
            // 	// Navigate to main app or dashboard
            // 	// await Shell.Current.GoToAsync("//MainApp");
            // }
            // else
            // {
            //     await DisplayAlertAsync("Error", "Invalid email or password. Try: demo@example.com / demo123", "OK");
            // }
            if (sessionValue == null)
            {
                return;
            }

            await Shell.Current.GoToAsync($"MainPage?cookies={Uri.EscapeDataString(sessionValue)}");

        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"An error occurred: {ex.Message}", "OK");
        }
        finally
        {
            // Re-enable button
            LoginButton.IsEnabled = true;
            LoginButton.Text = "Sign In";
        }
    }

    public async Task<string> LoginAndGetSessionAsync(string inputEmail, string password)
    {
        bool clientAdmin = false;
        try
        {
            string cookiesData = await handlerApi.readCookiesFile();
            // if (cookiesData != null)
            // {
                await DisplayAlertAsync("Info User", $"Cookies Ditemukan berisi: {cookiesData}", "OK");
            //     return cookiesData;
            // } else
            // {
                UserResponse usersData = await handlerApi.getInfoUser(inputEmail);

                if (usersData.status == "error")
                {
                    // check ke siswa
                    usersData = await handlerApi.getInfoUserSiswa(inputEmail);
                    if (usersData.status == "error")
                    {
                        await DisplayAlertAsync("Error", "User Tidak Ditemukan", "OK");
                        // return "";
                    }
                }
                await DisplayAlertAsync("Info User", usersData.status, "OK");
                await DisplayAlertAsync("Info User", $"Nama: {usersData.data.nama}\nEmail: {usersData.data.email}", "OK");

                bool verified = await verifyPassword(password, usersData.data.password);
                if (!verified)
                {
                    string? NISN = usersData.data.NISN.ToString();
                    bool isValidWithNISN = password == NISN;
                    if (isValidWithNISN)
                    {
                        await DisplayAlertAsync("Info User", NISN, "OK");
                    } else
                    {
                        await DisplayAlertAsync("Info User", "Password Salah", "OK");
                        return null;
                    }
                }

                var handler = new HttpClientHandler
                {
                    CookieContainer = new CookieContainer()
                };

                using var client = new HttpClient(handler);
                if (usersData.data.admin == "YA")
                {
                    clientAdmin = true;
                }

                await DisplayAlertAsync("Info User", $"Role: {usersData.data.admin}", "OK");
                var payload = new
                {
                    id = usersData.data.id,
                    username = usersData.data.nama,
                    email = usersData.data.email,
                    isAdmin = false,
                    role = usersData.data.admin == "TIDAK" ? "GURU" : "SISWA",
                    foto = usersData.data.foto,
                    kelas = usersData.data.kelas,
                    hashpassword = usersData.data.password,
                    no_telp = usersData.data.no_telp
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://nurulhuda.ilmeee.com/api/auth/login", content);
                var cookies = handler.CookieContainer.GetCookies(new Uri("https://nurulhuda.ilmeee.com"));
                await DisplayAlertAsync("Login Status", response.StatusCode.ToString(), "OK");
                foreach (Cookie cookie in cookies)
                {
                    if (cookie.Name == "auth_token")
                        return cookie.Value;
                }
            // }

            // string pw = "mySecretPassword";
            // string hashedPassword = BCrypt.Net.BCrypt.HashPassword(pw, 10);
            // await DisplayAlertAsync("Test hashedpassword", hashedPassword, "OK");

            
        }

        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Error: {ex}", "OK");
            // return "";
        }
        return null;
    }
    
    private async Task<bool> verifyPassword(string password, string hashedPassword)
    {
        if (hashedPassword == "")
        {
            await DisplayAlertAsync("Info verify password", "Masuk Menggunakan NISN", "Ok");
            return false;
        }
        bool isMatch = BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        return isMatch;
    }

    private async void OnForgotPasswordTapped(object sender, EventArgs e)
	{
		await DisplayAlertAsync("Forgot Password", "A password reset link will be sent to your email.", "OK");
		// Navigate to forgot password page or show reset form
	}

    private async void OnGoogleLoginClicked(object sender, EventArgs e)
    {
        await DisplayAlertAsync("Google Login", "Google authentication integration would be implemented here.", "OK");
        // Implement Google OAuth integration
    }

    private async void OnAppleLoginClicked(object sender, EventArgs e)
    {
        await DisplayAlertAsync("Apple Login", "Apple Sign-In integration would be implemented here.", "OK");
        // Implement Apple Sign-In integration
    }

    private async void OnSignUpTapped(object sender, EventArgs e)
    {
        await DisplayAlertAsync("Sign Up", "Navigate to registration page.", "OK");
        // Navigate to sign up page
        // await Shell.Current.GoToAsync("//SignUp");
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }


    public async Task<PermissionStatus> RequestLocationPermission()
    {
        PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

        if (status == PermissionStatus.Granted)
        {
            return status;
        }

        if (status == PermissionStatus.Denied && DeviceInfo.Platform == DevicePlatform.iOS)
        {
            // Prompt the user to go to settings on iOS if permission was previously denied
            // and they explicitly denied it (not just the first time).
            await Application.Current.MainPage.DisplayAlert("Permission Denied", "Please enable location services in settings.", "OK");
            return status;
        }

        status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

        return status;
    }

    private async void RequestCameraPermission()
    {
        var result = await Permissions.RequestAsync<Permissions.Camera>();

        if (result == PermissionStatus.Granted)
        {
            // Permission granted, proceed to use camera
        }
        else if (result == PermissionStatus.Denied)
        {
            // Handle case where user has denied the permission
        }
    }


    // public async Task<bool> CheckCameraPermissionAsync()
    // {
    //     var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
    //     if (status != PermissionStatus.Granted)
    //     {
    //         status = await Permissions.RequestAsync<Permissions.Camera>();
    //     }

    //     return status == PermissionStatus.Granted;
    // }

    // private async void OnOpenCameraClicked(object sender, EventArgs e)
    // {
    //     if (await CheckCameraPermissionAsync())
    //     {
    //         var photo = await MediaPicker.CapturePhotoAsync();
    //         if (photo != null)
    //         {
    //             await DisplayAlert("Foto", $"Path: {photo.FullPath}", "OK");
    //         }
    //     }
    //     else
    //     {
    //         await DisplayAlert("Permission", "Kamera tidak diizinkan", "OK");
    //     }
    // }
}