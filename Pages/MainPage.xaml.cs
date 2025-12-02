using System.Globalization;
using InventorySystem.Utils;

namespace InventorySystem.Pages;

public partial class MainPage : ContentPage
{
	private readonly BoxView Animator = new BoxView();

	private ApiHandler apiHandler;

	public MainPage()
	{
		InitializeComponent();
		apiHandler = new ApiHandler();
        Sidebar.IsVisible = _isSidebarOpen;
		LoadInventoryData();
	}

	private async void LoadInventoryData()
    {
        try
        {
            var response = await apiHandler.getInfoInventory();

            if (response.status == "success" && response.data != null)
            {
                // Set data ke CollectionView
                InventoryCollection.ItemsSource = response.data;
                UpdateStats(response.data);
            }
            else
            {
                await SnackBar.Show(response.message ?? "Failed to load data");
            }
        }
        catch (Exception ex)
        {
            await SnackBar.Show($"Failed to load inventory: {ex.Message}");
        }
    }

    private void UpdateStats(List<InventoryData> data)
    {
        // Total Items
        TotalItemsLabel.Text = data.Count.ToString();

        // Total Stock
        int totalStock = 0;
        foreach (var item in data)
        {
            if (int.TryParse(item.stock, out int stock))
            {
                totalStock += stock;
            }
        }
        TotalStockLabel.Text = totalStock.ToString("N0");

        // Total Value (stock * price)
        decimal totalValue = 0;
        foreach (var item in data)
        {
            if (int.TryParse(item.stock, out int stock) &&
                decimal.TryParse(item.price, out decimal price))
            {
                totalValue += (decimal)stock * price;
            }
        }
        TotalValueLabel.Text = $"Rp {totalValue:N2}";
    }

	private bool _isSidebarOpen = false;
	private async void OnClickSideBar(object sender, EventArgs e)
	{
		_isSidebarOpen = !_isSidebarOpen;
		await UpdateSidebarState();
	}

	private async Task UpdateSidebarState()
    {
        double width = 250; // width sidebar
        uint duration = 250;

        if (_isSidebarOpen)
        {
            Sidebar.IsVisible = true;
            // animate column dan sidebar bersamaan
            await Task.WhenAll(
                Sidebar.SlideInFromLeft(width, duration),
                Sidebar.FadeTo(1, duration)
            );
        }
        else
        {
            // keluar: animate column ke 0 dan sidebar slide out
            await Task.WhenAll(
                Sidebar.SlideOutToLeft(width, duration),
                Sidebar.FadeTo(0, duration)
            );

            Sidebar.IsVisible = false; // sembunyikan setelah selesai
        }
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        await apiHandler.clearCookiesFile();
        await Shell.Current.GoToAsync("//LoginPage");
    }
}
