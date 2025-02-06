namespace WebApplication7.Pages.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Count { get; set; }
        public decimal Price { get; set; } // Changed to decimal for prices
        public string ImageUrl { get; set; }
        public string Link { get; set; }
    }
}
