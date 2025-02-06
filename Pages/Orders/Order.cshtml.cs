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
        public string PaymentType { get; set; } // Выбор типа оплаты
        public string DeliveryType { get; set; } // Выбор типа доставки
        public string Comment { get; set; } // Комментарий пользователя
        public decimal TotalAmount { get; set; } // Общая сумма заказа
    }

    private readonly ICartService _cartService;
    private readonly ISessionCartService _sessionCartService;
    private readonly ApplicationDbContext _context; // Для работы с базой данных

    public OrderModel(ICartService cartService, ISessionCartService sessionCartService, ApplicationDbContext context)
    {
        _cartService = cartService;
        _sessionCartService = sessionCartService;
        _context = context; // Инициализируем контекст базы данных
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
            // Если пользователь не авторизован
            return RedirectToPage("/Index");
        }

        // Загрузка данных корзины
        CartItems = await _cartService.GetCartItemsAsync(UserId) ?? new List<CartItem>();

        if (!CartItems.Any())
        {
            TempData["Error"] = "Корзина пуста. Невозможно оформить заказ.";
            return RedirectToPage();
        }

        // Создание строки с названиями товаров
        var productNames = string.Join(";", CartItems.Select(item => item.ProductName));

        // Создание нового заказа
        var order = new Order
        {
            UserId = UserId,
            PaymentType = PaymentType,
            DeliveryType = DeliveryType,
            Comment = Comment,
            TotalAmount = TotalAmount,
            OrderItems = productNames // Названия товаров
        };

        // Добавление заказа в базу данных
        _context.Orders.Add(order);

        // Сохранение изменений
        await _context.SaveChangesAsync();

        // Очистка корзины после оформления заказа
        await _cartService.ClearCartAsync(UserId);

        return RedirectToPage("/Orders/OrderConfirmation"); // Перенаправление на страницу подтверждения
    }



}
