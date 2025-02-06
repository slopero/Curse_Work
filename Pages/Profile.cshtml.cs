namespace WebApplication7.Pages
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Data.Sqlite;
    using Microsoft.EntityFrameworkCore;
    using System.ComponentModel.DataAnnotations;
    using System.Security;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using WebApplication7.Pages.Models;
    using System.Collections.Generic;
    using System.Linq;

    public class ProfileModel : PageModel
    {
        [BindProperty]
        public string NewEmail { get; set; }

        [BindProperty]
        public string NewPassword { get; set; }

        [BindProperty]
        public string NewAvatarUrl { get; set; }

        [BindProperty]
        public string DeliveryAddress { get; set; }

        [BindProperty]
        public string PaymentMethod { get; set; }

        public List<OrderHistory> OrderHistory { get; set; } = new();

        public string Email = UserContext.Email;
        public string AvatarUrl = UserContext.AvatarUrl;
        string connectionString = "Data Source=E:/TUSUR/2 курс/OP/Work_variant/WebApplication7/WebApplication7/database/table4.db";
        public string UserId = UserContext.UserID;

        public async Task<IActionResult> OnGetAsync()
        {
            if (UserId == null)
            {
                return Redirect("/Auth");
            }
            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();

                var command = new SqliteCommand(
                    "SELECT OrderDate, OrderItems, TotalAmount FROM Orders WHERE UserId = @UserId ORDER BY OrderDate DESC",
                    connection);
                command.Parameters.AddWithValue("@UserId", UserContext.UserID);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    OrderHistory = new List<OrderHistory>();
                    while (await reader.ReadAsync())
                    {
                        OrderHistory.Add(new OrderHistory
                        {
                            OrderDate = reader.GetDateTime(0),
                            OrderItems = reader.GetString(1),
                            TotalAmount = reader.GetDecimal(2)
                        });
                    }
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateEmailAsync()
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                var command = new SqliteCommand("UPDATE users SET email = @NewEmail WHERE id = @UserId", connection);
                command.Parameters.AddWithValue("@NewEmail", NewEmail);
                command.Parameters.AddWithValue("@UserId", UserContext.UserID);
                await command.ExecuteNonQueryAsync();
                UserContext.Email = NewEmail;
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostUpdatePasswordAsync()
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                var command = new SqliteCommand("UPDATE users SET password = @NewPassword WHERE id = @UserId", connection);
                command.Parameters.AddWithValue("@NewPassword", NewPassword);
                command.Parameters.AddWithValue("@UserId", UserContext.UserID);
                await command.ExecuteNonQueryAsync();
                UserContext.Password = NewPassword;
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostUpdateAvatarAsync(IFormFile avatarFile)
        {
            if (avatarFile == null || avatarFile.Length == 0)
            {
                TempData["Error"] = "Файл не был загружен.";
                return RedirectToPage();
            }

            // Проверка расширения файла
            var allowedExtensions = new[] { ".jpg", ".jpeg" };
            var fileExtension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                TempData["Error"] = "Допускаются только файлы с расширением .jpg или .jpeg.";
                return RedirectToPage();
            }

            // Генерация пути для сохранения файла
            var fileName = $"{UserContext.UserID}{fileExtension}";
            var uploadPath = Path.Combine("wwwroot/images/Avatars", fileName);

            try
            {
                // Сохранение файла
                using (var stream = new FileStream(uploadPath, FileMode.Create))
                {
                    await avatarFile.CopyToAsync(stream);
                }

                // Обновление URL аватара в базе данных
                var newAvatarUrl = $"/images/Avatars/{fileName}";
                using (var connection = new SqliteConnection(connectionString))
                {
                    await connection.OpenAsync();
                    var command = new SqliteCommand("UPDATE users SET AvatarUrl = @NewAvatarUrl WHERE id = @UserId", connection);
                    command.Parameters.AddWithValue("@NewAvatarUrl", newAvatarUrl);
                    command.Parameters.AddWithValue("@UserId", UserContext.UserID);
                    UserContext.AvatarUrl = newAvatarUrl;
                    await command.ExecuteNonQueryAsync();
                }

                TempData["Success"] = "Аватар успешно обновлен.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Произошла ошибка: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateAddressAsync()
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                var command = new SqliteCommand("UPDATE users SET address = @DeliveryAddress WHERE id = @UserId", connection);
                command.Parameters.AddWithValue("@DeliveryAddress", DeliveryAddress);
                command.Parameters.AddWithValue("@UserId", UserContext.UserID);
                await command.ExecuteNonQueryAsync();
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostUpdatePaymentAsync()
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                var command = new SqliteCommand("UPDATE users SET payment = @PaymentMethod WHERE id = @UserId", connection);
                command.Parameters.AddWithValue("@PaymentMethod", PaymentMethod);
                command.Parameters.AddWithValue("@UserId", UserContext.UserID);
                await command.ExecuteNonQueryAsync();
                return RedirectToPage();
            }
        }
    }
}
