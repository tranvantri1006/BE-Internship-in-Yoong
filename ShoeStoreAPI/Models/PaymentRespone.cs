namespace ShoeStoreAPI.Models
{
    public class PaymentRespone
    {
        public int OrderId { get; set; }
        public string UserName { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public string Message { get; set; } = "";
    }
}
