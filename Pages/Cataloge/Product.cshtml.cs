using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.Data.Sqlite;
using WebApplication7.Pages.Models;

public class ProductsModel : PageModel
{
    public Boolean Exist = false;
    public string _connectionString = "Data Source=E:/TUSUR/2 курс/OP/Work_variant/WebApplication7/WebApplication7/database/table4.db";
    private readonly ApplicationDbContext _context;
    private readonly ISessionCartService _sessionCartService;
    private readonly ICartService _cartService;
    private readonly IWishlistService _wishlistService;
    private readonly ISessionWishlistService _sessionWishlistService;

    public ProductsModel(
        ApplicationDbContext context,
        ISessionCartService sessionCartService,
        ICartService cartService,
        IWishlistService wishlistService,
        ISessionWishlistService sessionWishlistService)
    {
        _context = context;
        _sessionCartService = sessionCartService;
        _cartService = cartService;
        _wishlistService = wishlistService;
        _sessionWishlistService = sessionWishlistService;
    }

    public List<Product> Products { get; set; } = new List<Product>();
    public decimal? MinPrice { get; set; } // Минимальная цена
    public decimal? MaxPrice { get; set; } // Максимальная цена

    public async Task OnGetAsync(decimal? minPrice, decimal? maxPrice, string searchQuery)
    {
        MinPrice = minPrice;
        MaxPrice = maxPrice;

        // Извлекаем все продукты
        var query = _context.Products.AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            // Приводим строку к нижнему регистру и ищем на клиенте
            query = query.ToList().Where(p => p.Name.ToLower().Contains(searchQuery.ToLower())).AsQueryable();
        }

        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        Products = query.ToList();
    }



    public async Task<IActionResult> OnPostAddToCartAsync(int productId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            return NotFound();
        }

        var cartItem = new CartItem
        {
            ProductId = productId,
            ProductName = product.Name,
            UnitPrice = product.Price,
            Quantity = 1,
            ImageUrl = product.ImageUrl
        };

        if (UserContext.UserID == null)
        {
            _sessionCartService.AddCartItem(cartItem);
        }
        else
        {
            _sessionCartService.AddCartItem(cartItem);
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SqliteCommand("INSERT INTO CartItems (UserId, ProductId, ProductName, UnitPrice, Quantity, ImageUrl) VALUES (@UserId, @ProductId, @ProductName, @UnitPrice, @Quantity, @ImageUrl)", connection);

                command.Parameters.AddWithValue("@UserId", UserContext.UserID);
                command.Parameters.AddWithValue("@ProductId", productId);
                command.Parameters.AddWithValue("@ProductName", product.Name);
                command.Parameters.AddWithValue("@UnitPrice", product.Price);
                command.Parameters.AddWithValue("@Quantity", 1);
                command.Parameters.AddWithValue("@ImageUrl", product.ImageUrl);

                await command.ExecuteNonQueryAsync();
            }
        }

        return StatusCode(201);
    }

    public async Task<IActionResult> OnPostAddToWishlistAsync(int productId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            return NotFound();
        }

        var wishlistItem = new WishlistItem
        {
            ProductId = productId,
            ProductName = product.Name,
            ImageUrl = product.ImageUrl,
            UnitPrice = product.Price
        };

        if (UserContext.UserID == null)
        {
            _sessionWishlistService.AddWishlistItem(wishlistItem);
        }
        else
        {
            _sessionWishlistService.AddWishlistItem(wishlistItem);
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();

                var checkCommand = new SqliteCommand("SELECT COUNT(*) FROM WishlistItems WHERE ProductId = @ProductId AND UserId = @UserId", connection);
                checkCommand.Parameters.AddWithValue("@UserId", UserContext.UserID);
                checkCommand.Parameters.AddWithValue("@ProductId", productId);

                var exists = (long)await checkCommand.ExecuteScalarAsync();

                if (exists == 0)
                {
                    Exist = true;
                    var insertCommand = new SqliteCommand("INSERT INTO WishlistItems (ProductId, UserId, ProductName, ImageUrl, UnitPrice) VALUES (@ProductId, @UserId, @ProductName, @ImageUrl, @UnitPrice)", connection);

                    insertCommand.Parameters.AddWithValue("@UserId", UserContext.UserID);
                    insertCommand.Parameters.AddWithValue("@ProductId", productId);
                    insertCommand.Parameters.AddWithValue("@ProductName", product.Name);
                    insertCommand.Parameters.AddWithValue("@UnitPrice", product.Price);
                    insertCommand.Parameters.AddWithValue("@ImageUrl", product.ImageUrl);

                    await insertCommand.ExecuteNonQueryAsync();
                }
            }
        }

        return RedirectToPage();
    }
}

