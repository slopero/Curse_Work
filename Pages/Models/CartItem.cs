namespace WebApplication7.Pages.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public string UserId { get; set; } 
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public string ImageUrl { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity;
    }
}
