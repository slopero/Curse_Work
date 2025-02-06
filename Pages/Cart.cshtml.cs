using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using WebApplication7.Pages.Models;

public class CartModel : PageModel
{
    private readonly ICartService _cartService;
    private readonly ISessionCartService _sessionCartService;

    public List<CartItem> CartItems { get; set; }

    public decimal TotalAmount
    {
        get
        {
            return CartItems?.Sum(item => item.TotalPrice) ?? 0;
        }
    }
    public CartModel(ICartService cartService, ISessionCartService sessionCartService)
    {
        _cartService = cartService;
        _sessionCartService = sessionCartService;
    }
    public string UserId = UserContext.UserID;
    public async Task<IActionResult> OnGetAsync()
    {
        UserId = UserContext.UserID;
        if (string.IsNullOrEmpty(UserId))
        {
            CartItems = _sessionCartService.GetCartItems();
        }
        else
        {
            CartItems = await _cartService.GetCartItemsAsync(UserId);
        }
        UserContext.Sum = CartItems.Sum(item => item.TotalPrice);

        return Page();
    }

    public async Task<IActionResult> OnPostCheckoutAsync()
    {
        if (UserId != null && CartItems != null )
        {
            TempData["Error"] = "Корзина пуста или пользователь не авторизован.";
            return RedirectToPage(); 
        }
        return RedirectToPage("/Orders/Order"); 
    }

    public async Task<IActionResult> OnPostAddToCartAsync(int productId, string productName, decimal unitPrice, string imageUrl)
    {

        var cartItem = new CartItem
        {
            ProductId = productId,
            ProductName = productName,
            UnitPrice = unitPrice,
            Quantity = 1,
            ImageUrl = imageUrl
        };

        if (string.IsNullOrEmpty(UserId))
        {
            // Add to session for unauthenticated users
            _sessionCartService.AddCartItem(cartItem);
        }
        else
        {
            // Add to database and session for authenticated users
            await _cartService.AddToCartAsync(UserId, cartItem);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateQuantityAsync(int productId, int quantity) 
    {
        if (string.IsNullOrEmpty(UserId))
        {
            // Update quantity in session for unauthenticated users
            _sessionCartService.UpdateCartItem(productId, quantity);
        }
        else
        {
            // Update quantity in database for authenticated users
            await _cartService.UpdateCartItemQuantityAsync(UserId, productId, quantity);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveFromCartAsync(int productId)
    {

        if (string.IsNullOrEmpty(UserId))
        {
            // Remove item from session for unauthenticated users
            _sessionCartService.RemoveCartItem(productId);
        }
        else
        {
            // Remove item from database for authenticated users
            await _cartService.RemoveFromCartAsync(UserId, productId);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostClearCartAsync()
    {

        if (string.IsNullOrEmpty(UserId))
        {
            // Clear session cart for unauthenticated users
            _sessionCartService.ClearCart();
        }
        else
        {
            // Clear database cart for authenticated users
            await _cartService.ClearCartAsync(UserId);
        }

        return RedirectToPage();
    }
}
