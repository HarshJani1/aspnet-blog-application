using aspnet_blog_application.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Security.Claims;

namespace aspnet_blog_application.Controllers
{
    public class AuthController : Controller
    {
        private readonly string connectionString = "server=localhost;user=root;database=blogapp;";

        public IActionResult Login() => View();
        public IActionResult Signup() => View();

        // ✅ Login Function
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            using var con = new MySqlConnection(connectionString);
            con.Open();
            using var cmd = con.CreateCommand();
            cmd.CommandText = "SELECT * FROM user WHERE email = @e AND password = @p";
            cmd.Parameters.AddWithValue("@e", email);
            cmd.Parameters.AddWithValue("@p", password);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                // ✅ Create claims for authentication
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, reader["name"].ToString()!),
                    new Claim(ClaimTypes.Email, reader["email"].ToString()!),
                    new Claim("UserId", reader["id"].ToString()!) // Important for associating posts
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                return RedirectToAction("Index", "Posts");
            }

            ViewBag.Error = "Invalid email or password.";
            return View();
        }

        // ✅ Signup Function
        [HttpPost]
        public IActionResult Signup(string name, string email, string password)
        {
            using var con = new MySqlConnection(connectionString);
            con.Open();

            // Check if email already exists
            using var checkCmd = con.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM user WHERE email = @e";
            checkCmd.Parameters.AddWithValue("@e", email);
            var exists = Convert.ToInt32(checkCmd.ExecuteScalar());

            if (exists > 0)
            {
                ViewBag.Error = "Email is already registered.";
                return View();
            }

            // Insert new user
            using var cmd = con.CreateCommand();
            cmd.CommandText = "INSERT INTO user (name, email, password) VALUES (@n, @e, @p)";
            cmd.Parameters.AddWithValue("@n", name);
            cmd.Parameters.AddWithValue("@e", email);
            cmd.Parameters.AddWithValue("@p", password);

            cmd.ExecuteNonQuery();

            // Redirect to login after signup
            return RedirectToAction("Login");
        }

        // ✅ Logout Function
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Login");
        }
    }
}
