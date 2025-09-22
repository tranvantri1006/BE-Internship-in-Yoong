namespace ShoeStoreAPI.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
