using Microsoft.Maui.Controls;
using System;
using System.Text;
using System.Net.Http;
using System.Net;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;

namespace InventorySystem.Pages;

public partial class LoginPage : ContentPage
{
    ApiHandler handlerApi = new ApiHandler();
    public LoginPage()
    {
        isLoggined();
        // if not loggined, show login page
        InitializeComponent();
    }

    private async void isLoggined()
    {
        var session = await handlerApi.readCookiesFile();
        if (!string.IsNullOrEmpty(session))
        {
            await Shell.Current.GoToAsync("//MainPage");
        }
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        LoginButton.IsEnabled = false;

        try
        {
            string email = EmailEntry.Text?.Trim();
            string password = PasswordEntry.Text;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                await SnackBar.Show("Please enter your username and password.");
                return;
            }
            
            LoginButton.Text = "Signing in...";

            await handlerApi.saveUserName(email);
            string sessionValue = await LoginAndGetSessionAsync(email, password);

            if (sessionValue != null)
            {
                if (RememberMeCheckBox.IsChecked)
                {
                    await handlerApi.saveCookiesFile(sessionValue);
                }
                await Shell.Current.GoToAsync("//MainPage");
            }
            else
            {
                await SnackBar.Show("Invalid username or password.");
            }
        }
        catch (Exception)
        {
            await SnackBar.Show("An error occurred. Please try again.");
        }
        finally
        {
            LoginButton.IsEnabled = true;
            LoginButton.Text = "Sign In";
        }
    }

    public async Task<string> LoginAndGetSessionAsync(string inputEmail, string password)
    {
        try
        {
            var payload = new Dictionary<string, string>
            {
                {"username", inputEmail},
                {"password", password}
            };

            // await SnackBar.Show($"Logging in... with email: {inputEmail} and password: {password}");
            var apiResponse = await handlerApi.Post(ApiHandler.LoginUrl, payload);
            var responseContent = await apiResponse.Response.Content.ReadAsStringAsync();

            // await SnackBar.Show($"Response: {responseContent}");

            if (apiResponse.Response.IsSuccessStatusCode)
            {
                var cookies = apiResponse.Cookies.GetCookies(new Uri(ApiHandler.BaseUrl));
                foreach (Cookie cookie in cookies)
                {
                    if (cookie.Name == "session") // Assuming the session cookie is named "session"
                    {
                        return cookie.Value;
                    }
                }
            }
        }
        catch (Exception)
        {
            await SnackBar.Show("An error occurred. Please try again.");
        }
        return null;
    }
    
    private async void OnForgotPasswordTapped(object sender, EventArgs e)
	{
		await SnackBar.Show("Forgot password functionality is not yet implemented.");
	}

    private async void OnSignUpTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//RegisterPage");
    }

    // Di LoginPage.xaml.cs
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Fade in animation
        this.Opacity = 0;
        await this.FadeTo(1, 100);
    }

    private bool _isPasswordVisible = false;

    private void OnTogglePasswordClicked(object sender, EventArgs e)
    {
        _isPasswordVisible = !_isPasswordVisible;

        PasswordEntry.IsPassword = !_isPasswordVisible;

        PasswordToggle.Source = _isPasswordVisible 
            ? "isvisible.png" 
            : "isnotvisible.png";
    }

}
