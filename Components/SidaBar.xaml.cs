using InventorySystem.Pages;
using InventorySystem.Utils;

namespace InventorySystem.Components;

public partial class SideBar : ContentView
{
    private ApiHandler apiHandler;
    private bool _isSidebarOpen = false;
    public Color DashboardButtonColor { get; set; }
    public Color SettingsButtonColor { get; set; }
    public Color DashboardTextColor { get; set; }
    public Color SettingsTextColor { get; set; }

    public SideBar()
    {
        InitializeComponent();
        apiHandler = new ApiHandler();
        Sidebar.IsVisible = _isSidebarOpen;
        // Set initial values for properties
        DashboardButtonColor = Color.FromHex("#00BFA5");
        SettingsButtonColor = Colors.Transparent;
        DashboardTextColor = Color.FromHex("#ffffff");
        SettingsTextColor = Color.FromHex("#666666");
    }

	public async void OnClickSideBar(object sender, EventArgs e)
	{
		Toggle();
	}

    // Public method untuk toggle sidebar
    public void Toggle()
    {
        _isSidebarOpen = !_isSidebarOpen;
        _ = UpdateSidebarState();
    }

    public bool IsOpen
    {
        get => _isSidebarOpen;
        set
        {
            _isSidebarOpen = value;
            _ = UpdateSidebarState();
        }
    }

	private void OnOverlayTapped(object sender, EventArgs e)
    {
        if (_isSidebarOpen)
        {
            _isSidebarOpen = false;
            _ = UpdateSidebarState();
        }
    }

    public async Task UpdateSidebarState()
    {
        double width = 250;
        uint duration = 250;

        if (_isSidebarOpen)
        {
            Sidebar.IsVisible = true;
            Overlay.IsVisible = true;
            await Task.WhenAll(
                Sidebar.SlideInFromLeft(width, duration),
                Sidebar.FadeTo(1, duration),
                Overlay.FadeTo(0.3, duration)
            );
        }
        else
        {
            await Task.WhenAll(
                Sidebar.SlideOutToLeft(width, duration),
                Sidebar.FadeTo(0, duration),
                Overlay.FadeTo(0, duration)
            );
            Sidebar.IsVisible = false;
            Overlay.IsVisible = false;
        }
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        await apiHandler.clearCookiesFile();
        await Shell.Current.GoToAsync("LoginPage");
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        DashboardButtonColor = Colors.Transparent;
        SettingsButtonColor = Color.FromHex("#00BFA5");
        DashboardTextColor = Color.FromHex("#666666");
        SettingsTextColor = Color.FromHex("#ffffff");
        await Shell.Current.GoToAsync("SettingPage");
    }

    private async void OnDashboardClicked(object sender, EventArgs e)
    {
        DashboardButtonColor = Color.FromHex("#00BFA5");
        SettingsButtonColor = Colors.Transparent;
        DashboardTextColor = Color.FromHex("#ffffff");
        SettingsTextColor = Color.FromHex("#666666");
        await Shell.Current.GoToAsync("///MainPage");
    }
}