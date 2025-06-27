using aspnet_blog_application.Models;
using aspnet_blog_application.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Diagnostics;

namespace aspnet_blog_application.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly string connectionString = "server=localhost;user=root;database=blogapp;";

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var model = GetAllPostsForYou();
            return View(model);
        }

        private PostViewModel GetAllPostsForYou()
        {
            List<PostModel> postList = new();

            using (var connection = new MySqlConnection(connectionString))
            {
                using (var command = connection.CreateCommand())
                {
                    connection.Open();
                    command.CommandText = @"SELECT p.Id, p.Title, p.Body, p.CreatedAt, p.UpdatedAt, u.Name 
                                            FROM post p 
                                            JOIN user u ON p.UserId = u.Id 
                                            ORDER BY p.UpdatedAt ASC"; // Change ASC to DESC if needed

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            postList.Add(new PostModel
                            {
                                Id = reader.GetInt32("Id"),
                                Title = reader.GetString("Title"),
                                Body = reader.GetString("Body"),
                                CreatedAt = reader.GetDateTime("CreatedAt"),
                                UpdatedAt = reader.GetDateTime("UpdatedAt"),
                                UserName = reader.GetString("Name") // 🟢 Added uploader's name
                            });
                        }
                    }
                }
            }

            return new PostViewModel { PostList = postList };
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
