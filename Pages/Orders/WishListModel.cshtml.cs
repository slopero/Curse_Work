using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication7.Pages.Models;

namespace WebApplication7.Pages
{
    public class WishlistModel : PageModel
    {
        private readonly IWishlistService _wishlistService;
        private readonly ISessionWishlistService _sessionWishlistService;

        public WishlistModel(IWishlistService wishlistService, ISessionWishlistService sessionWishlistService)
        {
            _wishlistService = wishlistService;
            _sessionWishlistService = sessionWishlistService;
        }

        public List<WishlistItem> WishlistItems { get; set; }
        public string UserId = UserContext.UserID;

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(UserId))
            {
                WishlistItems = _sessionWishlistService.GetWishlistItems();
            }
            else
            {
                WishlistItems = await _wishlistService.GetWishlistItemsAsync(UserId);
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAddToWishlistAsync(int productId, string productName, string imageUrl)
        {
            var wishlistItem = new WishlistItem
            {
                ProductId = productId,
                ProductName = productName,
                ImageUrl = imageUrl
            };

            if (string.IsNullOrEmpty(UserId))
            {
                _sessionWishlistService.AddWishlistItem(wishlistItem);
            }
            else
            {
                await _wishlistService.AddToWishlistAsync(UserId, wishlistItem);
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveFromWishlistAsync(int productId)
        {
            if (string.IsNullOrEmpty(UserId))
            {
                _sessionWishlistService.RemoveWishlistItem(productId);
            }
            else
            {
                await _wishlistService.RemoveFromWishlistAsync(UserId, productId);
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostClearWishlistAsync()
        {
            if (string.IsNullOrEmpty(UserId))
            {
                _sessionWishlistService.ClearWishlist();
            }
            else
            {
                await _wishlistService.ClearWishlistAsync(UserId);
            }

            return RedirectToPage();
        }
    }
}
