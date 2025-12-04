using Microsoft.Maui.Controls;
using System;
using System.Text;
using System.Net.Http;
using Newtonsoft.Json;

namespace InventorySystem.Pages;

public partial class RegisterPage : ContentPage
{
    private readonly ApiHandler _apiHandler = new ApiHandler();

    public RegisterPage()
    {
        InitializeComponent();
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        string username = UsernameEntry.Text?.Trim();
        string password = PasswordEntry.Text;
        string verifyPassword = VerifyPasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(verifyPassword) || string.IsNullOrWhiteSpace(password))
        {
            await SnackBar.Show("Please fill out all fields.");
            return;
        }

        if (password != verifyPassword)
        {
            await SnackBar.Show("Passwords do not match.");
            return;
        }

        try
        {
            var payload = new Dictionary<string, string>
            {
                {"username", username},
                {"password", password}
            };

            var response = await _apiHandler.Post(ApiHandler.RegisterUrl, payload);

            if (response.Response.IsSuccessStatusCode)
            {
                await Shell.Current.GoToAsync("//LoginPage");
            }
            else
            {
                await SnackBar.Show("Registration failed. Please try again.");
            }
        }
        catch (Exception)
        {
            await SnackBar.Show("An error occurred. Please try again.");
        }
    }

    private async void OnSignInTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//LoginPage");
    }
}
