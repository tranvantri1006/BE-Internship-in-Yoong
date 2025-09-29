namespace ShoeStoreAPI.Models
{
    public class PaymentRequest
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public List<PaymentItem> Items { get; set; } = new();
    }
}
