namespace SmartTemplateCore.Models.Common
{
    public class ChatRequest
    {
        public List<MessageChatBot> Messages { get; set; }
    }
    public class MessageChatBot
    {
        public string role { get; set; }
        public string content { get; set; }
    }
}
