namespace InventorySystem.GlobalVariables
{
#region Gemini Request
    public class GeminiRequest
    {
        public List<Content> Contents { get; set; }
    }

    public class Content
    {
        public List<Part>? parts { get; set; }
        public string? role { get; set; }
    }

    public class Part
    {
        public string? text { get; set; }
    }

    public class UsageMetadata
    {
        public int promptTokenCount { get; set; }
        public int candidatesTokenCount { get; set; }
        public int totalTokenCount { get; set; }
    }
#endregion
    public class ChatMessage
    {
        public string Content { get; set; } = "";
        public string MessageBgColor { get; set; } = "#F5F5F5";
        public string MessageTextColor { get; set; } = "Black";
    }

    #region Chat History Helper
    public class ChatHistoryResponse
    {
        public string status { get; set; }
        public string message { get; set; }
        public List<ChatHistoryData>? data { get; set; }  
    }

    public class ChatHistoryData
    {    
        public string? ID { get; set; }
        public string? created_at { get; set; }
        public string? request { get; set; }
        public string? response { get; set; }
        public string? created_by { get; set; }
    }

    #endregion
}