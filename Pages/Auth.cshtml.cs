using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using System.Threading.Tasks;
using WebApplication7.Pages.Models;

namespace WebApplication7.Pages
{
    [IgnoreAntiforgeryToken]
    public class Auth : PageModel
    {
        [BindProperty]
        public string email { get; set; }
        public string UserID { get; set; }
        public string AvatarUrl { get; set; }
        [BindProperty]
        public string password { get; set; }
        public string Message { get; set; }

        public void OnGet()
        {
            // Проверка сессии для предотвращения повторной авторизации
            if (HttpContext.Session.GetString("UserEmail") != null)
            {
                TempData["Notification"] = "Вы уже авторизованы";
                Response.Redirect("/index");
            }
        }

        public async Task<IActionResult> OnPostAsync(string Email, string Password)
        {
            Email = email;
            Password = password;
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                Message = "Email и пароль не могут быть пустыми.";
                return BadRequest(new { Message });
            }

            string connectionString = "Data Source=E:/TUSUR/2 курс/OP/Work_variant/WebApplication7/WebApplication7/database/table4.db";
            string sqlExpression = "SELECT id, email, password, AvatarUrl FROM users WHERE email = @Email AND password = @Password";

            try
            {
                using (var connection = new SqliteConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqliteCommand(sqlExpression, connection))
                    {
                        command.Parameters.AddWithValue("@Email", Email);
                        command.Parameters.AddWithValue("@Password", Password);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (!reader.HasRows)
                            {
                                Message = "Неверный логин или пароль.";
                                return Unauthorized();
                            }

                            await reader.ReadAsync();
                            UserContext.UserID = reader.GetInt32(0).ToString();
                            UserContext.Email = Email;
                            UserContext.Password = Password;
                            UserContext.AvatarUrl = reader["AvatarUrl"].ToString();

                            HttpContext.Session.SetString("UserEmail", Email);
                            HttpContext.Session.SetString("Id", UserContext.UserID);

                            // Уведомляем пользователя об успешной авторизации и перенаправляем на главную страницу
                            Message = "Успешная авторизация.";
                            return RedirectToPage("/index");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                Console.WriteLine($"Ошибка: {ex.Message}");
                Message = "Произошла ошибка на сервере.";
                return StatusCode(500, new { Message });
            }
        }
    }
}
