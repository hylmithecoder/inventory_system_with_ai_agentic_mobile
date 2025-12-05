using System.Globalization;
using InventorySystem.Utils;
using InventorySystem.GlobalVariables;
using System.Collections.ObjectModel;
using InventorySystem.Controls;

namespace InventorySystem.Pages;

public partial class MainPage : ContentPage
{
	private readonly BoxView Animator = new BoxView();
    public ObservableCollection<ChatMessage> Messages { get; set; } = new();
	private ApiHandler apiHandler;
    private HelperSql sqlHelper;
    private InventoryResponse inventoryData;
    private ChatHistoryResponse historyResponse;
    private List<InventoryData> _originalInventory;
    public string token;
    public string username;
    public string tableModelJsonFormat;
    private string finishedSqlScript;
    private string requestUser;
    private string responseToUser;
    private string currentMethod;

	public MainPage()
	{
		InitializeComponent();
		apiHandler = new ApiHandler();
        inventoryData = new InventoryResponse();
        sqlHelper = new HelperSql();
        Sidebar.IsVisible = _isSidebarOpen;
		LoadInventoryData();
        GetUserName();
        GetHistoryChat();
        BindingContext = this;
	}

    private async void GetUserName()
    {
        var getUsername = await apiHandler.readUserName();
        username = getUsername ?? "";
        await SnackBar.Show($"Welcome, {username}!");
    }

	private async void LoadInventoryData()
    {
        try
        {
            var response = await apiHandler.getInfoInventory();
            inventoryData = response;

            if (inventoryData.status == "success" && inventoryData.data != null)
            {
                // Set data ke CollectionView
                InventoryCollection.ItemsSource = inventoryData.data;
                var getToken = apiHandler.readCookiesFile();
                if (getToken != null)
                {
                    token = await getToken;
                    // await SnackBar.Show($"Token {token}");
                }
                // await SnackBar.Show("Inventory data loaded successfully.");
                UpdateStats(inventoryData.data);
                GetTableModel();
            }
            else
            {
                await SnackBar.Show(inventoryData.message ?? "Failed to load data");
            }
        }
        catch (Exception ex)
        {
            await SnackBar.Show($"Failed to load inventory: {ex.Message}");
        }
    }

    private void OnSearchClicked(object sender, EventArgs e)
    {
        string filter = SearchEntry.Text?.ToLower() ?? "";

        // kalau kosong → reset full data + update stats
        // if (string.IsNullOrWhiteSpace(filter))
        // {
        //     InventoryCollection.ItemsSource = _originalInventory;
        //     // UpdateStats(_originalInventory);
        //     return;
        // }

        // kalau ada filter → jalanin pencarian
        if (InventoryCollection.ItemsSource is List<InventoryData> allItems)
        {
            var filteredItems = allItems.FindAll(item =>
                item.name.ToLower().Contains(filter) ||
                item.ID.ToLower().Contains(filter));

            InventoryCollection.ItemsSource = filteredItems;
        }
        // var filteredItems = _originalInventory.FindAll(item =>
        //     item.name.ToLower().Contains(filter) ||
        //     item.ID.ToLower().Contains(filter));

        // InventoryCollection.ItemsSource = filteredItems;
    }

    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        string filter = e.NewTextValue?.ToLower() ?? "";

        // // kalau kosong → reset full data + update stats
        if (string.IsNullOrWhiteSpace(filter))
        {
            InventoryCollection.ItemsSource = _originalInventory;
            await SnackBar.Show("Reset full data + update stats");
            UpdateStats(_originalInventory);
            return;
        }

        // // kalau ada filter → jalanin pencarian
        // if (InventoryCollection.ItemsSource is List<InventoryData> allItems)
        // {
        //     var filteredItems = allItems.FindAll(item =>
        //         item.name.ToLower().Contains(filter) ||
        //         item.ID.ToLower().Contains(filter));

        //     InventoryCollection.ItemsSource = filteredItems;
        // }
        // var filteredItems = _originalInventory.FindAll(item =>
        //     item.name.ToLower().Contains(filter) ||
        //     item.ID.ToLower().Contains(filter));

        // InventoryCollection.ItemsSource = filteredItems;
    }

    private async void UpdateStats(List<InventoryData> data) { 
        // Total Items 
        TotalItemsLabel.Text = data.Count.ToString(); 
        //Total Stock 
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
            if (int.TryParse(item.stock, out int stock) && decimal.TryParse(item.price, out decimal price)) 
            { 
                int formattedPrice = (int)price / 100;
                // await SnackBar.Show($"Price of: {item.name} is {formattedPrice}");
                totalValue += stock * formattedPrice; 
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
    private bool _isItemTapped = false;
    private async void OnItemTapped(object sender, EventArgs e)
    {
        if (_isItemTapped) return; // Prevent multiple taps
        _isItemTapped = true;
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

    private async void OnChoiceLayoutTapped(object sender, EventArgs e)
    {
        // Cek apakah tap terjadi di luar ChoiceFrame
        var tapEventArgs = e as TappedEventArgs;
        if (tapEventArgs != null)
        {
            var position = tapEventArgs.GetPosition(ChoiceLayout);
            var frameBounds = new Rect(
                AbsoluteLayout.GetLayoutBounds(ChoiceFrame).X,
                AbsoluteLayout.GetLayoutBounds(ChoiceFrame).Y,
                ChoiceFrame.Width,
                ChoiceFrame.Height);

            if (!new Rect(frameBounds.X, frameBounds.Y, frameBounds.Width, frameBounds.Height).Contains(position.Value))
            {
                // Tutup modal jika tap di luar frame
                OnCancelChoiceClicked(sender, e);
            }
        }
    }

    private async void OnCancelChoiceClicked(object sender, EventArgs e)
    {
        NameEntry.Text = "";
        StockEntry.Text = "";
        PriceEntry.Text = "";

        await Task.WhenAll(
            ChoiceModal.FadeTo(0, 200, Easing.CubicOut),
            ChoiceModal.ScaleTo(0.8, 200, Easing.CubicIn)
        );

        ChoiceModal.IsVisible = false;
        _isItemTapped = false;
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

    private bool _isEditing = false;
    private async void OnEditItemClicked(object sender, EventArgs e)
    {
        _isEditing = true;
        ModalTitle.Text = "Edit Item";
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

        NameEntry.Text = "";
        StockEntry.Text = "";
        PriceEntry.Text = "";

        AddItemModal.IsVisible = false;
        _isItemTapped = false;
    }

    private async void OnSaveItemClicked(object sender, EventArgs e)
    {
        try
        {
            var payload = new Dictionary<string, string>
            {
                {"name", NameEntry.Text?.Trim()},
                {"stock", StockEntry.Text?.Trim()},
                {"price", PriceEntry.Text?.Trim()},
                {"created_by", username},
                {"action", "create"}
            };

            if (_isEditing && ChoiceModal.BindingContext is InventoryData editItem)
            {
                payload["id"] = editItem.ID ?? "";
                payload["_method"] = "PUT";
                var response = await apiHandler.Post(ApiHandler.BaseUrl, payload);
                var responseContent = await response.Response.Content.ReadAsStringAsync();
                // await SnackBar.Show($"Response: {responseContent}");

                if (response.Response.IsSuccessStatusCode)
                {
                    LoadInventoryData();
                }
                else
                {
                    await SnackBar.Show("Failed to add item. Please try again.");
                }
                // await SnackBar.Show("Is editing true");
                _isEditing = false;

            } 
            else 
            {
                // await SnackBar.Show($"Payload: {string.Join(", ", payload.Select(kv => $"{kv.Key}={kv.Value}"))}");
                var response = await apiHandler.Post(ApiHandler.BaseUrl, payload);
                var responseContent = await response.Response.Content.ReadAsStringAsync();
                // await SnackBar.Show($"Response: {responseContent}");
                
                if (response.Response.IsSuccessStatusCode)
                {
                    LoadInventoryData();
                }
                else
                {
                    await SnackBar.Show("Failed to add item. Please try again.");
                }
                // await SnackBar.Show("Is editing false");
            }
        } 
        catch (Exception ex)
        {
            await SnackBar.Show($"Failed to add item: {ex.Message}");
        }
        finally
        {
            OnCancelAddClicked(sender, e);
        }
    }

    private async void OnDeleteItemClicked(object sender, EventArgs e)
    {
        try
        {
            if (ChoiceModal.BindingContext is InventoryData item)
            {

                string deleteUrl = $"{ApiHandler.BaseUrl}?token={token}&id={item.ID}";
                var response = await apiHandler.Delete(deleteUrl);

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
        finally
        {
            OnCancelChoiceClicked(sender, e);
        }
    }

    #region Chatbot handler
    private bool _isChatbotOpen = false;

    private async void OnMessageClicked(object sender, EventArgs e)
    {
        // await SnackBar.Show("Chatbot feature is under development.");
        _isChatbotOpen = !_isChatbotOpen;
        UpdateChatbotState();
    }

    private async Task UpdateChatbotState()
    {
        // await SnackBar.Show("Chatbot feature is under development.");
        double width = 300;
        uint duration = 250;

        if (_isChatbotOpen)
        {
            await Task.Delay(100);
            
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
            await Task.Delay(100);

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
        if (string.IsNullOrEmpty(message))
        {
            return;
        }
        
        AddMessageToChat($"You: {message}", false);
        requestUser = message;
        // await SnackBar.Show($"Sending message to Gemini: {message}");
        ChatInput.Text = "";

        try
        {
            string currentTableModel = tableModelJsonFormat;
            var geminiPayload = new GeminiRequest
            {
                Contents = new List<Content>
                {
                    new Content
                    {
                        role = "user",
                        parts = new List<Part> { new Part { text = $"{message}, you just create with a sql script only skip all the explain and commentar, the table model is {currentTableModel}" }}
                    }
                }
            };

            // await SnackBar.Show("Sending message to Gemini...");
            var response = await apiHandler.PostGemini(geminiPayload);
            if (response.candidates != null)
            {
                // await SnackBar.Show("Received response from Gemini.");
                string botReply = response.candidates[0]?.content?.parts[0]?.text ?? "Chatbot: (tidak ada respons)";
                // string getSqlScript = sqlHelper.wrapSqlScript(botReply);
                // await SnackBar.Show($"Received response from Gemini: {botReply}");
                finishedSqlScript = sqlHelper.RemoveSignScript(botReply);
                await SnackBar.Show($"Received response from Gemini: {finishedSqlScript}");
                string naturalLangConfirm = sqlHelper.ConvertSqlScriptToNaturalLanguage(finishedSqlScript);
                responseToUser = naturalLangConfirm;
                ConfirmModalTitle.Text = naturalLangConfirm;
                AnimateConfirmModal();
                // AddMessageToChat($"SQl Script {getSqlScript}", false);
                AddMessageToChat($"Bot: {botReply}", true);
            }
            else
            {
                await SnackBar.Show("Kosong");
                AddMessageToChat("Bot: No response from Gemini.", true);
            }
        }
        catch (Exception ex)
        {
            AddMessageToChat($"Bot: Error occurred - {ex.Message}", true);
        }
    }

    bool _isConfirmModalOpen = false;
    private async void AnimateConfirmModal()
    {
        if (_isConfirmModalOpen) return;

        ConfirmModal.IsVisible = true;

        // reset state sebelum animasi
        ConfirmModal.Opacity = 0;
        ConfirmModal.Scale = 0.8;

        // animasi fade + zoom in
        await Task.WhenAll(
            ConfirmModal.FadeTo(1, 250, Easing.CubicIn),
            ConfirmModal.ScaleTo(1, 250, Easing.CubicOut)
        );
    }

    private async void OnConfirmLayoutTapped(object sender, EventArgs e)
    {
        AnimateDownConfirmModal();
    }

    private async void OnChatbotCloseClicked(object sender, EventArgs e)
    {
        OnChatbotOverlayTapped(sender, e);
        // await SnackBar.Show("Chatbot closed.");
    }

    private void AddMessageToChat(string text, bool isAi)
    {
        // Tambah pesan baru
        if (isAi)
        {
            Messages.Add(new ChatMessage
            {
                Content = text,
                MessageBgColor = "#4e65e3ff",   // contoh warna bubble
                MessageTextColor = "#333333"
            });
        }
        else
        {
            Messages.Add(new ChatMessage
            {
                Content = text,
                MessageBgColor = "#E0F7FA",   // contoh warna bubble
                MessageTextColor = "#333333"
            });           
        }
    }
    #endregion

    #region Helper SQL
    private async void GetTableModel()
    {
        var payload = new Dictionary<string, string>
        {
            {"type", "tables"},
            {"token", token}
        };

        var response = await apiHandler.Post(ApiHandler.getAllInformDatabase, payload);
        var json = await response.Response.Content.ReadAsStringAsync();
        string jsonFormat = sqlHelper.FormattedJsonFile(json);
        tableModelJsonFormat = jsonFormat;
    }


    private async void GetHistoryChat()
    {
        var payload = new Dictionary<string, string>
        {
            {"type", "history_chat"},
            {"token", token}
        };

        var reponse = await apiHandler.getInfoHistory(payload);
        historyResponse = reponse;

        foreach (ChatHistoryData data in historyResponse.data)
        {
            AddMessageToChat(data.request, false);
            AddMessageToChat(data.response, true);
        }
    }

    private async void OnConfirmYesClicked(object sender, EventArgs e)
    {
        try
        {
            var payload = new Dictionary<string, string>
            {
                {"token", token},
                {"action", "ask_ai"},
                {"request", requestUser},
                {"response", responseToUser},
                {"sql_script", finishedSqlScript}
            };

            var response = await apiHandler.Post(ApiHandler.HandlerRequest, payload);
            if (!response.Response.IsSuccessStatusCode)
            {
                await SnackBar.Show("Failed to add item. Please try logout and login again.");
            }

            var json = await response.Response.Content.ReadAsStringAsync();
            await SnackBar.Show($"Response: {json}");
            LoadInventoryData();
        }
        catch (Exception ex)
        {
            await SnackBar.Show($"Failed to add item: {ex.Message}");
        }
        finally
        {
            AnimateDownConfirmModal();
        }
    }

    private async void OnCancelConfirmClicked(object sender, EventArgs e)
    {
        AnimateDownConfirmModal();
    }

    private async void AnimateDownConfirmModal()
    {
        await Task.WhenAll(
            ConfirmModal.FadeTo(0, 250, Easing.CubicIn),
            ConfirmModal.ScaleTo(0.8, 250, Easing.CubicOut)
        );
        ConfirmModal.IsVisible = false;
    }

    #endregion
}
