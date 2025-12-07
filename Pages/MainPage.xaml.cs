using System.Globalization;
using InventorySystem.Utils;
using InventorySystem.GlobalVariables;
using System.Collections.ObjectModel;
using InventorySystem.Controls;
using Newtonsoft.Json.Linq;
using CommunityToolkit.Maui.Media;
using CommunityToolkit.Maui.Alerts;
using InventorySystem.Components;
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
    private string username, tableModelJsonFormat, finishedSqlScript, requestUser, responseToUser, token;
    public string recognitedText { get; set; } = string.Empty;
    private readonly ISpeechToText speechToText;
    private bool _isListening = false, _isSidebarOpen = false, _isItemTapped = false, _isEditing = false,  _isChatbotOpen = false, _isConfirmModalOpen = false, _isClickConfirmed = false;
    private CancellationTokenSource _cts;

	public MainPage(ISpeechToText speechToText)
	{
		InitializeComponent();
		apiHandler = new ApiHandler();
        inventoryData = new InventoryResponse();
        sqlHelper = new HelperSql();
        _cts = new CancellationTokenSource();
        // SideBar.IsVisible = _isSidebarOpen;
		LoadInventoryData();
        GetUserName();
        GetHistoryChat();
        RequestMicrophonePermissionAsync();
        BindingContext = this;
        this.speechToText = speechToText;
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
            var response = await apiHandler.getInfoInventory($"{ApiHandler.BaseUrl}?user={username}");
            inventoryData = response;

            if (inventoryData.status == "success" && inventoryData.data != null)
            {
                // Set data ke CollectionView
                _originalInventory = inventoryData.data;
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

        if (InventoryCollection.ItemsSource is List<InventoryData> allItems)
        {
            var filteredItems = allItems.FindAll(item =>
                item.name.ToLower().Contains(filter) ||
                item.ID.ToLower().Contains(filter));

            InventoryCollection.ItemsSource = filteredItems;
        }
    }

    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        string filter = e.NewTextValue?.ToLower() ?? "";

        if (string.IsNullOrWhiteSpace(filter))
        {
            InventoryCollection.ItemsSource = _originalInventory;
            return;
        }
    }

    private async void UpdateStats(List<InventoryData> data)
    {
        // Total Items
        TotalItemsLabel.Text = data.Count.ToString();

        // Total Stock
        long totalStock = 0; // pakai long
        foreach (var item in data)
        {
            if (long.TryParse(item.stock, out long stock))
            {
                totalStock += stock;
            }
        }
        TotalStockLabel.Text = totalStock.ToString("N0");

        // Total Value
        decimal totalValue = 0;
        foreach (var item in data)
        {
            if (long.TryParse(item.stock, out long stock) && decimal.TryParse(item.price, out decimal price))
            {
                // jangan cast ke int, langsung pakai decimal
                decimal formattedPrice = price / 100m;
                totalValue += stock * formattedPrice;
            }
        }
        TotalValueLabel.Text = $"Rp {totalValue:N0}";
    }

	private async void OnClickSideBar(object sender, EventArgs e)
	{
		SideBar.IsOpen = true;
	}

    // Button Item Tapped
    private async void OnItemTapped(object sender, EventArgs e)
    {
        if (_isItemTapped) return;
        _isItemTapped = true;

        if (sender is Border border && border.BindingContext is InventoryData item)
        {
            // var bounds = AbsoluteLayout.GetLayoutBounds(border);

            // // Reset the position of ChoiceFrame to its original position
            // AbsoluteLayout.SetLayoutBounds(ChoiceFrame,
            //     new Rect(0, bounds.Y + border.Height, ChoiceFrame.Width, ChoiceFrame.Height));

            ChoiceModal.IsVisible = true;
            ChoiceModal.BindingContext = item;

            // Reset state
            ChoiceModal.Opacity = 0;
            ChoiceModal.Scale = 0.7;

            // Fade + wave scale
            await ChoiceModal.FadeTo(1, 200, Easing.CubicIn);

            // Wave effect: scale up → scale down → settle
            await ChoiceModal.ScaleTo(1.1, 150, Easing.SinOut);
            await ChoiceModal.ScaleTo(0.95, 150, Easing.SinIn);
            await ChoiceModal.ScaleTo(1.0, 150, Easing.CubicOut);

            _isItemTapped = false;
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
                var response = await apiHandler.Post($"{ApiHandler.BaseUrl}/", payload);
                var responseContent = await response.Response.Content.ReadAsStringAsync();

                if (response.Response.IsSuccessStatusCode)
                {
                    LoadInventoryData();
                    await SnackBar.Show("Successfully edited item.");
                }
                else
                {
                    await SnackBar.Show("Failed to add item. Please try again.");
                }
                _isEditing = false;

            } 
            else 
            {
                var response = await apiHandler.Post(ApiHandler.BaseUrl+"/", payload);
                var responseContent = await response.Response.Content.ReadAsStringAsync();
                
                if (response.Response.IsSuccessStatusCode)
                {
                    LoadInventoryData();
                    await SnackBar.Show("Successfully added item.");
                }
                else
                {
                    await SnackBar.Show("Failed to add item. Please try again.");
                }
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

                string deleteUrl = $"{ApiHandler.BaseUrl}/?token={token}&id={item.ID}";
                var response = await apiHandler.Delete(deleteUrl);

                var responseContent = await response.Response.Content.ReadAsStringAsync();

                if (response.Response.IsSuccessStatusCode)
                {
                    LoadInventoryData();
                    await SnackBar.Show("Successfully deleted item.");
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
    private async void OnMessageClicked(object sender, EventArgs e)
    {
        _isChatbotOpen = !_isChatbotOpen;
        UpdateChatbotState();
    }

    private async Task UpdateChatbotState()
    {
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
            // kalau lagi listening, tunggu sampai RecognitionText terisi
            if (!_isListening)
            {
                await StartListening();
                _isListening = true;
                SendButton.Source = "unmicrophone.png"; // indikator stop mic
            }
            else
            {
                await StopListening();
                _isListening = false;
                SendButton.Source = "microphone.png"; // kembali ke mic
            }
            return;
        }
        
        AddMessageToChat($"{message}", false);
        requestUser = message;
        // await SnackBar.Show($"Sending message to Gemini: {message}");
        ChatInput.Text = "";
        SendButton.Source = "microphone.png";

        try
        {
            string currentTableModel = tableModelJsonFormat;
            string formattedRequest = 
                "Kamu adalah AI yang hanya menjawab dalam format JSON valid.\n" +
                "Format wajib:\n" +
                "{\n" +
                "  \"response\": \"penjelasan singkat dalam bahasa manusia (tanpa menyebut SQL, query, atau script secara eksplisit)\",\n" +
                "  \"sql_script\": \"query SQL yang dihasilkan atau null jika tidak relevan\"\n" +
                "}\n\n" +

                "Data Pendukung:\n" +
                $"- Nama Pengguna: {username} (dipakai untuk kolom created_by)\n" +
                $"- User message: {message}\n" +
                $"- Table model: {currentTableModel}\n\n" +

                "Aturan keamanan:\n" +
                "- Tabel \"account\" hanya digunakan untuk memeriksa apakah pengguna adalah admin.\n" +
                "- Kolom tanda admin adalah \"isAdmin\" yang nilainya \"yes\" atau \"no\".\n" +
                "- Untuk permintaan yang memodifikasi data (INSERT, UPDATE, DELETE):\n" +
                "  • jika isAdmin = \"yes\", jalankan sesuai aturan normal.\n" +
                "  • jika isAdmin = \"no\" atau pengguna tidak ditemukan:\n" +
                "      - \"sql_script\" harus bernilai null\n" +
                "      - \"response\" jelaskan singkat bahwa pengguna tidak memiliki izin admin.\n" +
                "- Jangan sebutkan detail tabel pada \"response\".\n" +
                "- Jangan keluarkan SQL jika pengguna bukan admin.\n\n" +

                "Aturan tambahan:\n" +
                "- Jika user bertanya di luar konteks tabel, tetap balas JSON di atas, dan isi \"sql_script\" null.\n" +
                "- Jangan berikan jawaban lain selain JSON.\n";


            var geminiPayload = new GeminiRequest
            {
                Contents = new List<Content>
                {
                    new Content
                    {
                        role = "user",
                        parts = new List<Part> { new Part { text = $"{formattedRequest}" } },
                    }
                }
            };

            // await SnackBar.Show("Sending message to Gemini...");
            var response = await apiHandler.PostGemini(geminiPayload);
            if (response.candidates != null)
            {
                string botReply = response.candidates[0]?.content?.parts[0]?.text ?? "Chatbot: (tidak ada respons)";
                
                var jsonRes = JObject.Parse(sqlHelper.RemoveSignScript(botReply));
                var naturalLangConfirm = jsonRes["response"].ToString();
                finishedSqlScript = jsonRes["sql_script"].ToString();

                responseToUser = naturalLangConfirm;

                ConfirmModalTitle.Text = naturalLangConfirm;
                // if (!string.IsNullOrEmpty(finishedSqlScript))
                // {                
                // await SnackBar.Show($"SQL Script: {finishedSqlScript}");
                AnimateConfirmModal();
                // }
                AddMessageToChat($"{naturalLangConfirm}", true);
            }
            else
            {
                AddMessageToChat("No response from Gemini.", true);
            }
        }
        catch (Exception ex)
        {
            AddMessageToChat($"Error occurred - {ex.Message}", true);
        }
    }

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
                MessageBgColor = "#4e65e3ff",
                MessageTextColor = "#34c6eb",
                MessageAlignment = "Left"
            });
        }
        else
        {
            Messages.Add(new ChatMessage
            {
                Content = text,
                MessageBgColor = "#E0F7FA",
                MessageTextColor = "#333333",
                MessageAlignment = "Right"
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
            if (_isClickConfirmed) {
                await SnackBar.Show("Please wait...");
                return;
            }

            _isClickConfirmed = true;
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

            await SnackBar.Show($"Successfully executed item.");
            LoadInventoryData();
        }
        catch (Exception ex)
        {
            await SnackBar.Show($"Failed to add item: {ex.Message}");
        }
        finally
        {
            AnimateDownConfirmModal();
            _isClickConfirmed = false;
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

    #region Input ask handler
    private void OnChatInputTextChanged(object sender, TextChangedEventArgs e)
    {
        string input = e.NewTextValue;
        if (string.IsNullOrEmpty(input))
        {
            SendButton.Source = "microphone.png";
        } 
        else
        {
            SendButton.Source = "send.png";    
        }
    }

    #endregion

    #region Mic Permission
    public static async Task<bool> RequestMicrophonePermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Microphone>();
        }
        return status == PermissionStatus.Granted;
    }

    private async Task StartListening()
    {
        var cancellationToken = _cts.Token;

        var isGranted = await speechToText.RequestPermissions(cancellationToken);
        if (!isGranted)
        {
            Toast.Make("Permission not granted").Show();
            return;
        }  

        speechToText.RecognitionResultUpdated += OnRecognitionTextUpdated;
        speechToText.RecognitionResultCompleted += OnRecognitionTextCompleted;
        await speechToText.StartListenAsync(new SpeechToTextOptions { Culture = CultureInfo.CurrentCulture, ShouldReportPartialResults = true });
    }

    private async Task StopListening()
    {
        var cancellationToken = _cts.Token;

        await speechToText.StopListenAsync();
        speechToText.RecognitionResultUpdated -= OnRecognitionTextUpdated;
        speechToText.RecognitionResultCompleted -= OnRecognitionTextCompleted;
    }

    private void OnRecognitionTextUpdated(object? sender, SpeechToTextRecognitionResultUpdatedEventArgs args)
    {
        var result = args.RecognitionResult; // ini tipe SpeechToTextResult
        string recognizedText = result; // ambil teks dari property Text

        if (!string.IsNullOrEmpty(recognizedText))
            ChatInput.Text = recognizedText;
    }

    private void OnRecognitionTextCompleted(object? sender, SpeechToTextRecognitionResultCompletedEventArgs args)
    {
        var result = args.RecognitionResult; // ini tipe SpeechToTextResult
        string recognizedText = result.Text; // ambil teks dari property Text

        if (!string.IsNullOrEmpty(recognizedText))
            ChatInput.Text = recognizedText;
    }

    #endregion

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (SideBar != null)
        {
            SideBar.DashboardButtonColor = Color.FromHex("#00BFA5");
            SideBar.DashboardTextColor = Color.FromHex("#ffffff");
            SideBar.SettingsButtonColor = Colors.Transparent;
            SideBar.SettingsTextColor = Color.FromHex("#666666");
        }
    }

}
