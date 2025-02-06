using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;

namespace WebApplication7.Pages
{
    [IgnoreAntiforgeryToken]
    public class Reg : PageModel
    {
        [BindProperty]
        public string Email { get; set; }
        [BindProperty]
        public string Password { get; set; }
        public string Message { get; set; }
        public string URL { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Проверяем, что поля не пустые
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                Message = "Email и пароль не могут быть пустыми.";
                return Page();
            }

            // Дополнительная проверка формата Email
            if (!Email.Contains("@") || !Email.Contains("."))
            {
                Message = "Введите корректный адрес электронной почты.";
               return Page();
            }

            string connectionString = "Data Source=E:/TUSUR/2 курс/OP/Work_variant/WebApplication7/WebApplication7/database/table4.db";
            string checkEmailQuery = "SELECT COUNT(*) FROM users WHERE email = @Email";

            try
            {
                using (var connection = new SqliteConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Проверяем, существует ли пользователь с таким email
                    using (var command = new SqliteCommand(checkEmailQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Email", Email);
                        var emailCount = (long)await command.ExecuteScalarAsync();

                        if (emailCount > 0)
                        {
                            Message = "Пользователь с таким email уже существует.";
                            return Page();
                        }
                    }

                    // Установка URL для аватара
                    URL = "/images/base_avatar.jpg";

                    // Добавление нового пользователя
                    string insertUserQuery = "INSERT INTO users (email, password, AvatarUrl) VALUES (@Email, @Password, @URL)";
                    using (var command = new SqliteCommand(insertUserQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Email", Email);
                        command.Parameters.AddWithValue("@Password", Password);
                        command.Parameters.AddWithValue("@URL", URL);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                // Успешная регистрация
                Message = "Пользователь успешно зарегистрирован.";
                return RedirectToPage("/Index"); // Перенаправление на главную страницу
            }
            catch (Exception ex)
            {
                // Логируем и возвращаем сообщение об ошибке
                Message = "Произошла ошибка при обработке запроса.";
                return StatusCode(500, new { Message, Error = ex.Message });
            }
        }
    }
}
