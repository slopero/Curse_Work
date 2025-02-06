namespace WebApplication7.Pages.Models
{
    using System;

    public class Order
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string PaymentType { get; set; }
        public string DeliveryType { get; set; }
        public string Comment { get; set; }
        public decimal TotalAmount { get; set; }
        public string OrderItems { get; set; }

    }

}
