using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using aspnet_blog_application.Models;
using aspnet_blog_application.Models.ViewModels;
using MySql.Data.MySqlClient;
using Microsoft.AspNetCore.Authorization;

namespace aspnet_blog_application.Controllers;

[Authorize]
public class PostsController : Controller
{
    private readonly ILogger<PostsController> _logger;
    private readonly string connectionString = "server=localhost;user=root;database=blogapp;";
    private readonly IConfiguration _configuration;

    public PostsController(ILogger<PostsController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    // ✅ Show All Posts for Logged-in User
    public IActionResult Index()
    {
        var postListViewModel = GetAllPosts();
        return View(postListViewModel);
    }

    public IActionResult NewPost()
    {
        return View();
    }

    public IActionResult EditPost(int id)
    {
        var post = GetPostById(id);
        if (post == null)
            return NotFound();

        return View(new PostViewModel { Post = post });
    }

    public IActionResult ViewPost(int id)
    {
        var post = GetPostById(id);
        if (post == null)
            return NotFound();

        return View(new PostViewModel { Post = post });
    }

    // ✅ Fetch post by Id (only if belongs to user)
    internal PostModel? GetPostById(int id)
    {
        var userId = HttpContext.User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userId))
            return null;

        PostModel post = null;

        using (var connection = new MySqlConnection(connectionString))
        {
            using (var command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = "SELECT * FROM post WHERE Id = @id AND UserId = @userId";
                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@userId", userId);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        post = new PostModel
                        {
                            Id = reader.GetInt32("Id"),
                            Title = reader.GetString("Title"),
                            Body = reader.GetString("Body"),
                            CreatedAt = reader.GetDateTime("CreatedAt"),
                            UpdatedAt = reader.GetDateTime("UpdatedAt")
                        };
                    }
                }
            }
        }

        return post;
    }

    // ✅ Fetch All Posts for Logged-in User
    internal PostViewModel GetAllPosts()
    {
        List<PostModel> postList = new();
        var userId = HttpContext.User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userId))
            return new PostViewModel { PostList = postList };

        using (var connection = new MySqlConnection(connectionString))
        {
            using (var command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = "SELECT * FROM post WHERE UserId = @userId";
                command.Parameters.AddWithValue("@userId", userId);

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
                            UpdatedAt = reader.GetDateTime("UpdatedAt")
                        });
                    }
                }
            }
        }

        return new PostViewModel { PostList = postList };
    }

    // ✅ Insert New Post (with UserId)
    public ActionResult Insert(PostModel post)
    {
        var userId = HttpContext.User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        post.CreatedAt = DateTime.Now;
        post.UpdatedAt = DateTime.Now;

        using (var connection = new MySqlConnection(connectionString))
        {
            using (var command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = @"INSERT INTO post (Title, Body, CreatedAt, UpdatedAt, UserId) 
                                        VALUES (@title, @body, @createdAt, @updatedAt, @userId)";
                command.Parameters.AddWithValue("@title", post.Title);
                command.Parameters.AddWithValue("@body", post.Body);
                command.Parameters.AddWithValue("@createdAt", post.CreatedAt);
                command.Parameters.AddWithValue("@updatedAt", post.UpdatedAt);
                command.Parameters.AddWithValue("@userId", userId);

                command.ExecuteNonQuery();
            }
        }

        return RedirectToAction(nameof(Index));
    }

    // ✅ Update Post (only if belongs to user)
    public ActionResult Update(PostModel post)
    {
        var userId = HttpContext.User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        post.UpdatedAt = DateTime.Now;

        using (var connection = new MySqlConnection(connectionString))
        {
            using (var command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = @"UPDATE post 
                                        SET Title = @title, Body = @body, UpdatedAt = @updatedAt 
                                        WHERE Id = @id AND UserId = @userId";
                command.Parameters.AddWithValue("@title", post.Title);
                command.Parameters.AddWithValue("@body", post.Body);
                command.Parameters.AddWithValue("@updatedAt", post.UpdatedAt);
                command.Parameters.AddWithValue("@id", post.Id);
                command.Parameters.AddWithValue("@userId", userId);

                command.ExecuteNonQuery();
            }
        }

        return RedirectToAction(nameof(Index));
    }

    // ✅ Delete Post (only if belongs to user)
    [HttpPost]
    public JsonResult Delete(int id)
    {
        var userId = HttpContext.User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Json(new { success = false });

        using (var connection = new MySqlConnection(connectionString))
        {
            using (var command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = "DELETE FROM post WHERE Id = @id AND UserId = @userId";
                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@userId", userId);

                command.ExecuteNonQuery();
            }
        }

        return Json(new { success = true });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
