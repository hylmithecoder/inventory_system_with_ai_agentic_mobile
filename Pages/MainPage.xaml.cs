using System.Globalization;
namespace InventorySystem.Pages;

public static class AnimationExtensions
{
    public static Task SlideInFromLeft(this VisualElement element, double targetWidth, uint length = 250)
    {
        element.AbortAnimation("SlideIn");
        var tcs = new TaskCompletionSource<bool>();

        // Setup awal
        element.AnchorX = 0; // pivot kiri
        element.InputTransparent = true;
        element.IsVisible = true;
        element.WidthRequest = targetWidth;
        element.TranslationX = -targetWidth;

        // Animasi dengan easing yang halus
        element.Animate(
            name: "SlideIn",
            callback: v => element.TranslationX = v,
            start: -targetWidth,
            end: 0,
            length: length,
            easing: Easing.CubicOut,
            finished: (v, cancelled) =>
            {
                element.TranslationX = 0;
                element.InputTransparent = false;
                tcs.SetResult(!cancelled);
            }
        );

        return tcs.Task;
    }

    public static Task SlideOutToLeft(this VisualElement element, double targetWidth, uint length = 250)
    {
        element.AbortAnimation("SlideOut");
        var tcs = new TaskCompletionSource<bool>();

        element.AnchorX = 0;
        element.InputTransparent = true;
        // pastikan start posisi 0 (visible area)
        element.TranslationX = 0;

        element.Animate(
            name: "SlideOut",
            callback: v => element.TranslationX = v,
            start: 0,
            end: -targetWidth,
            length: length,
            easing: Easing.CubicIn,
            finished: (v, cancelled) =>
            {
                element.TranslationX = -targetWidth;
                element.InputTransparent = false;
                tcs.SetResult(!cancelled);
            }
        );

        return tcs.Task;
    }

	public static Task WidthTo(this ColumnDefinition column, double newWidth, View animator, uint length = 250)
	{
		double startWidth = column.Width.Value;

		var tcs = new TaskCompletionSource<bool>();

		animator.Animate(
			name: "ColumnWidthAnimation",
			callback: value =>
			{
				column.Width = new GridLength(value);
			},
			start: startWidth,
			end: newWidth,
			length: length,
			easing: Easing.Linear,
			finished: (v, c) => tcs.SetResult(true)
		);

		return tcs.Task;
	}
}

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
				// await DisplayAlert("Success", $"Inventory data loaded successfully {response.data.Count}", "OK");
				await DisplayAlert("Success", $"List first item {response.data[0].name}", "OK");
                InventoryCollection.ItemsSource = response.data;
                // Hitung statistik
                UpdateStats(response.data);
            }
            else
            {
                await DisplayAlert("Error", response.message ?? "Failed to load data", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load inventory: {ex.Message}", "OK");
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

    private void OnClickMeTapped(object sender, EventArgs e)
    {
        PopupOverlay.IsVisible = true;
    }

    private void OnPopupCloseTapped(object sender, EventArgs e)
    {
        PopupOverlay.IsVisible = false;
    }

    private void OnPopupOverlayTapped(object sender, EventArgs e)
    {
        PopupOverlay.IsVisible = false;
    }

	private bool _isSidebarOpen = false;
	private async void OnClickSideBar(object sender, EventArgs e)
	{
		_isSidebarOpen = !_isSidebarOpen;
		UpdateSidebarState();
	}

	private async void UpdateSidebarState()
    {
        double width = 250; // width sidebar
        uint duration = 250;

        if (_isSidebarOpen)
        {
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

}
