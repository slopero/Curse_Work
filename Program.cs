
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using WebApplication7.Pages.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure database context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
           .EnableSensitiveDataLogging() // Enable query logging
           .LogTo(Console.WriteLine));   // Log queries to the console

// Configure Identity


// Configure JWT Authentication



// Configure Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register services
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ISessionCartService, SessionCartService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ISessionWishlistService, SessionWishlistService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication(); // Must be before UseAuthorization
app.UseAuthorization();
app.MapRazorPages();
app.Run();

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    public DbSet<Product> Products { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<WishlistItem> WishlistItems { get; set; }
}




public interface ICartService
{
    Task<List<CartItem>> GetCartItemsAsync(string userId);
    Task AddToCartAsync(string userId, CartItem item);
    Task UpdateCartItemQuantityAsync(string userId, int productId, int newQuantity);
    Task RemoveFromCartAsync(string userId, int productId);
    Task ClearCartAsync(string userId);
}




public interface IWishlistService
{
    Task<List<WishlistItem>> GetWishlistItemsAsync(string userId);
    Task AddToWishlistAsync(string userId, WishlistItem item);
    Task RemoveFromWishlistAsync(string userId, int productId);
    Task ClearWishlistAsync(string userId);
}
public class WishlistService : IWishlistService
{
    private readonly ApplicationDbContext _context;
    private readonly ISessionWishlistService _sessionWishlistService;

    public WishlistService(ApplicationDbContext context, ISessionWishlistService sessionWishlistService)
    {
        _context = context;
        _sessionWishlistService = sessionWishlistService;
    }

    public async Task<List<WishlistItem>> GetWishlistItemsAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return _sessionWishlistService.GetWishlistItems();
        }
        return await _context.WishlistItems.Where(item => item.UserId == userId).ToListAsync();
    }

    public async Task AddToWishlistAsync(string userId, WishlistItem item)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            _sessionWishlistService.AddWishlistItem(item);
            return;
        }

        var existingItem = await _context.WishlistItems.FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == item.ProductId);

        if (existingItem == null)
        {
            item.UserId = userId;
            await _context.WishlistItems.AddAsync(item);
            await _context.SaveChangesAsync();
        }
    }

    public async Task RemoveFromWishlistAsync(string userId, int productId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            _sessionWishlistService.RemoveWishlistItem(productId);
            return;
        }

        var item = await _context.WishlistItems.FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);
        if (item != null)
        {
            _context.WishlistItems.Remove(item);
            await _context.SaveChangesAsync();
        }
    }

    public async Task ClearWishlistAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            _sessionWishlistService.ClearWishlist();
            return;
        }

        var items = await _context.WishlistItems.Where(w => w.UserId == userId).ToListAsync();
        _context.WishlistItems.RemoveRange(items);
        await _context.SaveChangesAsync();
    }
}

public interface ISessionWishlistService
{
    void AddWishlistItem(WishlistItem item);
    void RemoveWishlistItem(int productId);
    List<WishlistItem> GetWishlistItems();
    void ClearWishlist();
}

public class SessionWishlistService : ISessionWishlistService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string SessionKey = "Wishlist";

    public SessionWishlistService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void AddWishlistItem(WishlistItem item)
    {
        var wishlist = GetWishlistItems();
        if (!wishlist.Any(w => w.ProductId == item.ProductId))
        {
            wishlist.Add(item);
            SaveWishlist(wishlist);
        }
    }

    public void RemoveWishlistItem(int productId)
    {
        var wishlist = GetWishlistItems();
        var itemToRemove = wishlist.FirstOrDefault(w => w.ProductId == productId);
        if (itemToRemove != null)
        {
            wishlist.Remove(itemToRemove);
            SaveWishlist(wishlist);
        }
    }

    public List<WishlistItem> GetWishlistItems()
    {
        var wishlist = _httpContextAccessor.HttpContext.Session.GetString(SessionKey);
        return string.IsNullOrEmpty(wishlist) ? new List<WishlistItem>() : JsonConvert.DeserializeObject<List<WishlistItem>>(wishlist);
    }

    public void ClearWishlist()
    {
        _httpContextAccessor.HttpContext.Session.Remove(SessionKey);
    }

    private void SaveWishlist(List<WishlistItem> wishlist)
    {
        _httpContextAccessor.HttpContext.Session.SetString(SessionKey, JsonConvert.SerializeObject(wishlist));
    }
}

public interface ISessionCartService
{
    List<CartItem> GetCartItems();
    void AddCartItem(CartItem item);
    void UpdateCartItem(int productId, int quantity);
    void RemoveCartItem(int productId);
    void ClearCart();
}

public class SessionCartService : ISessionCartService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string CartSessionKey = "CartSession";

    public SessionCartService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public List<CartItem> GetCartItems()
    {
        var session = _httpContextAccessor.HttpContext.Session;
        var cartItemsJson = session.GetString(CartSessionKey);
        return string.IsNullOrEmpty(cartItemsJson) ? new List<CartItem>() : System.Text.Json.JsonSerializer.Deserialize<List<CartItem>>(cartItemsJson);
    }

    public void AddCartItem(CartItem item)
    {
        var cartItems = GetCartItems();
        var existingItem = cartItems.FirstOrDefault(c => c.ProductId == item.ProductId);
        if (existingItem != null)
        {
            existingItem.Quantity += item.Quantity;
        }
        else
        {
            cartItems.Add(item);
        }
        SaveCartItems(cartItems);
    }

    public void UpdateCartItem(int productId, int quantity)
    {
        var cartItems = GetCartItems();
        var existingItem = cartItems.FirstOrDefault(c => c.ProductId == productId);
        if (existingItem != null)
        {
            existingItem.Quantity++;
        }
        SaveCartItems(cartItems);
    }

    public void RemoveCartItem(int productId)
    {
        var cartItems = GetCartItems();
        cartItems.RemoveAll(c => c.ProductId == productId);
        SaveCartItems(cartItems);
    }

    public void ClearCart()
    {
        SaveCartItems(new List<CartItem>());
    }

    private void SaveCartItems(List<CartItem> cartItems)
    {
        var session = _httpContextAccessor.HttpContext.Session;
        var cartItemsJson = System.Text.Json.JsonSerializer.Serialize(cartItems);
        session.SetString(CartSessionKey, cartItemsJson);
    }
}

public class CartService : ICartService
{
    private readonly ApplicationDbContext _context;
    private readonly ISessionCartService _sessionCartService;

    public CartService(ApplicationDbContext context, ISessionCartService sessionCartService)
    {
        _context = context;
        _sessionCartService = sessionCartService; // Inject session service
    }

    public async Task<List<CartItem>> GetCartItemsAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            // Return session items for unauthenticated users
            return _sessionCartService.GetCartItems();
        }

        // Return database items for authenticated users
        return await _context.CartItems
            .Where(item => item.UserId == userId)
            .ToListAsync();
    }

    public async Task AddToCartAsync(string userId, CartItem item)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            // Add to session cart for unauthenticated users
            _sessionCartService.AddCartItem(item);
            return;
        }

        // Try to find existing item in the cart
        var existingItem = await _context.CartItems
            .FirstOrDefaultAsync(cartItem => cartItem.UserId == userId && cartItem.ProductId == item.ProductId);

        if (existingItem != null)
        {
            // Update quantity if the item already exists
            existingItem.Quantity += item.Quantity;
        }
        else
        {
            // Add new item if it doesn't exist
            item.UserId = userId;
            await _context.CartItems.AddAsync(item);
        }

        // Save changes to the database
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error saving to database", ex);
        }
    }

    public async Task UpdateCartItemQuantityAsync(string userId, int productId, int newQuantity)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            // Update session cart for unauthenticated users
            _sessionCartService.UpdateCartItem(productId, newQuantity);
            return;
        }

        var item = await _context.CartItems
            .FirstOrDefaultAsync(cartItem => cartItem.UserId == userId && cartItem.ProductId == productId);

        if (item != null)
        {
            item.Quantity = newQuantity;
            if (item.Quantity <= 0)
            {
                _context.CartItems.Remove(item);
            }

            await _context.SaveChangesAsync();
        }
    }

    public async Task RemoveFromCartAsync(string userId, int productId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            // Remove from session cart for unauthenticated users
            _sessionCartService.RemoveCartItem(productId);
            return;
        }

        var item = await _context.CartItems
            .FirstOrDefaultAsync(cartItem => cartItem.UserId == userId && cartItem.ProductId == productId);

        if (item != null)
        {
            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
        }
    }

    public async Task ClearCartAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            // Clear session cart for unauthenticated users
            _sessionCartService.ClearCart();
            return;
        }

        var items = await _context.CartItems
            .Where(cartItem => cartItem.UserId == userId)
            .ToListAsync();

        _context.CartItems.RemoveRange(items);
        await _context.SaveChangesAsync();
    }
}



