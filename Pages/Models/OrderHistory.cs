namespace WebApplication7.Pages.Models
{
    public class OrderHistory
    {
        public DateTime OrderDate { get; set; }
        public string OrderItems { get; set; }
        public decimal TotalAmount {  get; set; }
    }
}
