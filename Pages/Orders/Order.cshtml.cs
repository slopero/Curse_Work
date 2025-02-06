using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication7.Pages.Models;

public class OrderModel : PageModel
{
    public class OrderViewModel
    {
        public List<CartItem> CartItems { get; set; }
        public string PaymentType { get; set; } // ����� ���� ������
        public string DeliveryType { get; set; } // ����� ���� ��������
        public string Comment { get; set; } // ����������� ������������
        public decimal TotalAmount { get; set; } // ����� ����� ������
    }

    private readonly ICartService _cartService;
    private readonly ISessionCartService _sessionCartService;
    private readonly ApplicationDbContext _context; // ��� ������ � ����� ������

    public OrderModel(ICartService cartService, ISessionCartService sessionCartService, ApplicationDbContext context)
    {
        _cartService = cartService;
        _sessionCartService = sessionCartService;
        _context = context; // �������������� �������� ���� ������
    }

    [BindProperty]
    public List<CartItem> CartItems { get; set; }

    [BindProperty]
    public string PaymentType { get; set; }

    [BindProperty]
    public string DeliveryType { get; set; }

    [BindProperty]
    public string Comment { get; set; }

    [BindProperty]
    public decimal TotalAmount => CartItems?.Sum(item => item.TotalPrice) ?? 0;

    public string UserId = UserContext.UserID;

    public async Task<IActionResult> OnGetAsync()
    {
        UserId = UserContext.UserID;
        CartItems = UserId != null ? await _cartService.GetCartItemsAsync(UserId) : _sessionCartService.GetCartItems();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (UserId == null)
        {
            // ���� ������������ �� �����������
            return RedirectToPage("/Index");
        }

        // �������� ������ �������
        CartItems = await _cartService.GetCartItemsAsync(UserId) ?? new List<CartItem>();

        if (!CartItems.Any())
        {
            TempData["Error"] = "������� �����. ���������� �������� �����.";
            return RedirectToPage();
        }

        // �������� ������ � ���������� �������
        var productNames = string.Join(";", CartItems.Select(item => item.ProductName));

        // �������� ������ ������
        var order = new Order
        {
            UserId = UserId,
            PaymentType = PaymentType,
            DeliveryType = DeliveryType,
            Comment = Comment,
            TotalAmount = TotalAmount,
            OrderItems = productNames // �������� �������
        };

        // ���������� ������ � ���� ������
        _context.Orders.Add(order);

        // ���������� ���������
        await _context.SaveChangesAsync();

        // ������� ������� ����� ���������� ������
        await _cartService.ClearCartAsync(UserId);

        return RedirectToPage("/Orders/OrderConfirmation"); // ��������������� �� �������� �������������
    }



}
