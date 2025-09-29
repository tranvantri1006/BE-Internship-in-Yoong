namespace ShoeStoreAPI.Models
{
    public class Feedback
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public int ProductId { get; set; }
        public string Content { get; set; } = "";
        public int Rating { get; set; }   // số sao (1–5)
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
