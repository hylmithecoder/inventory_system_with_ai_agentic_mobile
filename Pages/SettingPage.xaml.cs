using InventorySystem.Components;
using InventorySystem.Controls;

namespace InventorySystem.Pages;

public partial class SettingPage : ContentPage
{
    private ApiHandler apiHandler;
    private string username;
    public SettingPage()
    {
        apiHandler = new ApiHandler();
        SetProfile();
        InitializeComponent();
    }

    private async void SetProfile()
    {
        username = await apiHandler.readUserName();
        UsernameEntry.Text = username;
    }

    private async void OnClickSideBar(object sender, EventArgs e)
	{
		SideBar.OnClickSideBar(sender, e);
	}

    private async void OnUpdatePasswordClicked(object sender, EventArgs e)
    {
        string getUsername = UsernameEntry.Text?.Trim();
        string current = CurrentPasswordEntry.Text?.Trim();
        string newPass = NewPasswordEntry.Text?.Trim();
        string confirm = ConfirmPasswordEntry.Text?.Trim();

        if (string.IsNullOrEmpty(getUsername) || string.IsNullOrEmpty(current) || string.IsNullOrEmpty(newPass) || string.IsNullOrEmpty(confirm))
        {
            await SnackBar.Show("All fields are required !!!");
            return;
        }

        if (newPass != confirm)
        {
            await SnackBar.Show("New password and confirmation do not match !!!");
            return;
        }

        var payload = new Dictionary<string, string>
        {
            {"username", getUsername},
            {"current_password", current},
            {"new_password", newPass},
            {"current_username", username},
            {"_method", "PUT"}
        };

        var success = await apiHandler.Post(ApiHandler.LoginUrl, payload);
        if (!success.Response.IsSuccessStatusCode)
        {
            await SnackBar.Show("Failed to update password !!!");
        }
        var reponse = await success.Response.Content.ReadAsStringAsync();

        await SnackBar.Show($"Password updated successfully. {reponse}");
        SetProfile();
    }

}
