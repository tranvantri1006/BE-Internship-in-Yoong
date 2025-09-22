namespace ShoeStoreAPI.Models
{
    public class Promotion
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;  // Tên chương trình
        public string Description { get; set; } = string.Empty; // Mô tả
        public string Location { get; set; } = string.Empty;  // Địa phương áp dụng
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
