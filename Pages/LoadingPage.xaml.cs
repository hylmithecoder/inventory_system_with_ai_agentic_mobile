using Microsoft.Maui.Controls;
using System;

namespace InventorySystem.Pages
{
    public partial class LoadingPage : ContentPage
    {
        public LoadingPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            var apiHandler = new ApiHandler();
            var session = await apiHandler.readCookiesFile();

            if (!string.IsNullOrEmpty(session))
            {
                await Shell.Current.GoToAsync("//MainPage");
            }
            else
            {
                await Shell.Current.GoToAsync("//LoginPage");
            }
        }
    }
}
