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

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        string filter = e.NewTextValue?.ToLower() ?? "";

        if (InventoryCollection.ItemsSource is List<InventoryData> allItems)
        {
            var filteredItems = allItems.FindAll(item =>
                item.name.ToLower().Contains(filter) ||
                item.ID.ToLower().Contains(filter));

            InventoryCollection.ItemsSource = filteredItems;
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
                totalValue += stock * price;
                // Format ulang harga agar tidak ada ".00"
                item.price = price.ToString("N0"); 
            }
        }
        TotalValueLabel.Text = $"Rp {totalValue:N0}";
    }


	private bool _isSidebarOpen = false;
	private async void OnClickSideBar(object sender, EventArgs e)
	{
		_isSidebarOpen = !_isSidebarOpen;
		await UpdateSidebarState();
	}

	private void OnOverlayTapped(object sender, EventArgs e)
    {
        if (_isSidebarOpen)
        {
            _isSidebarOpen = false;
            _ = UpdateSidebarState();
        }
    }

    private async Task UpdateSidebarState()
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
        await Shell.Current.GoToAsync("//LoginPage");
    }

    // Button Item Tapped
    private async void OnItemTapped(object sender, EventArgs e)
    {
        if (sender is Border border && border.BindingContext is InventoryData item)
        {
            // Ambil posisi relatif ke parent AbsoluteLayout
            var bounds = AbsoluteLayout.GetLayoutBounds(border);

            // Atur posisi ChoiceFrame dekat border
            AbsoluteLayout.SetLayoutBounds(ChoiceFrame,
                new Rect(bounds.X, bounds.Y + border.Height + 5, ChoiceFrame.Width, ChoiceFrame.Height));

            ChoiceModal.IsVisible = true;
            ChoiceModal.BindingContext = item;

            // Animasi muncul
            ChoiceModal.Opacity = 0;
            ChoiceModal.Scale = 0.8;
            await Task.WhenAll(
                ChoiceModal.FadeTo(1, 250, Easing.CubicIn),
                ChoiceModal.ScaleTo(1, 250, Easing.CubicOut)
            );
        }
    }

    private async void OnCancelChoiceClicked(object sender, EventArgs e)
    {
        await Task.WhenAll(
            ChoiceModal.FadeTo(0, 200, Easing.CubicOut),
            ChoiceModal.ScaleTo(0.8, 200, Easing.CubicIn)
        );

        ChoiceModal.IsVisible = false;
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        LoadInventoryData();
    }

    private async void OnAddItemClicked(object sender, EventArgs e)
    {
        AddItemModal.IsVisible = true;

        // reset state sebelum animasi
        AddItemModal.Opacity = 0;
        AddItemModal.Scale = 0.8;

        // animasi fade + zoom in
        await Task.WhenAll(
            AddItemModal.FadeTo(1, 250, Easing.CubicIn),
            AddItemModal.ScaleTo(1, 250, Easing.CubicOut)
        );
    }

    private async void OnEditItemClicked(object sender, EventArgs e)
    {
        if (ChoiceModal.BindingContext is InventoryData item)
        {
            // Isi data ke form edit
            NameEntry.Text = item.name;
            StockEntry.Text = item.stock;
            PriceEntry.Text = item.price;

            // Tutup choice modal
            await Task.WhenAll(
                ChoiceModal.FadeTo(0, 200, Easing.CubicOut),
                ChoiceModal.ScaleTo(0.8, 200, Easing.CubicIn)
            );
            ChoiceModal.IsVisible = false;

            // Tampilkan add item modal
            AddItemModal.IsVisible = true;

            // reset state sebelum animasi
            AddItemModal.Opacity = 0;
            AddItemModal.Scale = 0.8;

            // animasi fade + zoom in
            await Task.WhenAll(
                AddItemModal.FadeTo(1, 250, Easing.CubicIn),
                AddItemModal.ScaleTo(1, 250, Easing.CubicOut)
            );
        }
    }

    private async void OnCancelAddClicked(object sender, EventArgs e)
    {
        // animasi fade out + zoom out
        await Task.WhenAll(
            AddItemModal.FadeTo(0, 200, Easing.CubicOut),
            AddItemModal.ScaleTo(0.8, 200, Easing.CubicIn)
        );

        AddItemModal.IsVisible = false;
    }

    private async void OnSaveItemClicked(object sender, EventArgs e)
    {
        try
        {
            string currentUsername = apiHandler.username ?? "";
            var payload = new Dictionary<string, string>
            {
                {"name", NameEntry.Text?.Trim()},
                {"stock", StockEntry.Text?.Trim()},
                {"price", PriceEntry.Text?.Trim()},
                {"created_by", currentUsername}
            };

            var response = await apiHandler.Post(ApiHandler.BaseUrl, payload);
            var responseContent = await response.Response.Content.ReadAsStringAsync();
            await SnackBar.Show($"Response: {responseContent}");

            if (response.Response.IsSuccessStatusCode)
            {
                LoadInventoryData();
            }
            else
            {
                await SnackBar.Show("Failed to add item. Please try again.");
            }

        } catch (Exception ex)
        {
            await SnackBar.Show($"Failed to add item: {ex.Message}");
        }
    }

    private async void OnDeleteItemClicked(object sender, EventArgs e)
    {
        try
        {
            if (ChoiceModal.BindingContext is InventoryData item)
            {
                var response = await apiHandler.Put(ApiHandler.BaseUrl + "delete/", new Dictionary<string, string>
                {
                    {"id", item.ID}
                });

                var responseContent = await response.Response.Content.ReadAsStringAsync();
                await SnackBar.Show($"Response: {responseContent}");

                if (response.Response.IsSuccessStatusCode)
                {
                    LoadInventoryData();
                }
                else
                {
                    await SnackBar.Show("Failed to delete item. Please try again.");
                }
            }
        }
        catch (Exception ex)
        {
            await SnackBar.Show($"Failed to delete item: {ex.Message}");
        }
    }

    #region Chatbot handler
    private bool _isChatbotOpen = false;

    private async void OnMessageClicked(object sender, EventArgs e)
    {
        _isChatbotOpen = !_isChatbotOpen;
        await UpdateChatbotState();
    }

    private async Task UpdateChatbotState()
    {
        await SnackBar.Show("Chatbot feature is under development.");
        double width = 300;
        uint duration = 250;

        if (_isChatbotOpen)
        {
            ChatbotSidebar.IsVisible = true;
            ChatbotOverlay.IsVisible = true;
            await Task.WhenAll(
                ChatbotSidebar.SlideInFromRight(width, duration),
                ChatbotSidebar.FadeTo(1, duration),
                ChatbotOverlay.FadeTo(0.3, duration)
            );
        }
        else
        {
            await Task.WhenAll(
                ChatbotSidebar.SlideOutToRight(width, duration),
                ChatbotSidebar.FadeTo(0, duration),
                ChatbotOverlay.FadeTo(0, duration)
            );
            ChatbotSidebar.IsVisible = false;
            ChatbotOverlay.IsVisible = false;
        }
    }

    private void OnChatbotOverlayTapped(object sender, EventArgs e)
    {
        if (_isChatbotOpen)
        {
            _isChatbotOpen = false;
            _ = UpdateChatbotState();
        }
    }

    private async void OnSendMessageClicked(object sender, EventArgs e)
    {
        string message = ChatInput.Text?.Trim();
        if (!string.IsNullOrEmpty(message))
        {
            // Tambah pesan user ke chat
            ChatMessagesList.ItemsSource = null;
            var messages = new List<string>();
            if (ChatMessagesList.ItemsSource is List<string> existingMessages)
            {
                messages.AddRange(existingMessages);
            }
            messages.Add("User: " + message);

            // Clear input
            ChatInput.Text = "";

            // Simulasi balasan chatbot
            var payload = new Dictionary<string, object>
            {
                ["contents"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["role"] = "user",
                        ["parts"] = new List<object>
                        {
                            new Dictionary<string, object>
                            {
                                ["text"] = message
                            }
                        }
                    }
                }
            };

            await Task.Delay(1000); // Simulasi delay
            var response = await apiHandler.PostGemini(ApiHandler.GeminiUrl, payload);

            messages.Add("Chatbot: This is a simulated response.");

            ChatMessagesList.ItemsSource = messages;

            // Scroll ke bawah
            ChatMessagesList.ScrollTo(messages.Count - 1);
        }
    }

    private async void OnChatbotCloseClicked(object sender, EventArgs e)
    {
        // Clear chat messages
        ChatMessagesList.ItemsSource = null;
        await SnackBar.Show("Chat cleared.");
    }
    #endregion
}
