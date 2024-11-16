using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TodoApp.Controllers;
using TodoApp.Data;
using TodoApp.Dtos;
using TodoApp.Models;
using Xunit;

namespace TodoApp.Tests
{
    public class AdminControllerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly AdminController _controller;

        public AdminControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique name per instance)
                .Options;

            _context = new AppDbContext(options);
            _controller = new AdminController(_context);

            // Seed initial user data
            var user = new User
            {
                Id = 1,
                Username = "AdminUser",
                PasswordHash = new byte[] { 0x00 },
                PasswordSalt = new byte[] { 0x00 }
            };
            _context.Users.Add(user);

            // seed initial todo items
            var todoItem1 = new TodoItem
            {
                Id = 1,
                Title = "Todo Item 1",
                IsCompleted = false,
                UserId = user.Id,
            };
            _context.TodoItems.Add(todoItem1);

            // seed initial todo items
            var todoItem2 = new TodoItem
            {
                Id = 2,
                Title = "Todo Item 2",
                IsCompleted = true,
                UserId = user.Id,
            };
            _context.TodoItems.Add(todoItem2);

            _context.SaveChanges();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "AdminUser")
            };

            var httpUser = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = httpUser }
            };
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task GetUsernames_ReturnsListOfUsernames()
        {
            // Act
            var result = await _controller.GetUsernames();

            // Assert
            var actionResult = Assert.IsType<ActionResult<List<string>>>(result);
            var usernames = Assert.IsType<List<string>>(actionResult.Value);
            Assert.Single(usernames); // Initially seeded with one user
            Assert.Contains("AdminUser", usernames);
        }

        [Fact]
        public async Task GetUsernames_ReturnsUnauthorized_IfUserNotFound()
        {
            // Arrange
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

            // Act
            var result = await _controller.GetUsernames();

            // Assert
            var actionResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.Equal("User information is missing.", actionResult.Value);
        }

        [Fact]
        public async Task DeleteUser_RemovesUser()
        {
            // Assign 
            var user = new UserDeleteDto { Username = "AdminUser" };

            // Act
            var result = await _controller.DeleteUser(user);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Null(await _context.Users.FindAsync(1));
        }

        [Fact]
        public async Task DeleteUser_ReturnsNotFound_ForInvalidId()
        {
            // Assign
            var user = new UserDeleteDto { Username = "Invalid User" };
            // Act
            var result = await _controller.DeleteUser(user);

            // Assert
            var actionResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("User not found.", actionResult.Value);
        }

        [Fact]
        public async Task DeleteUser_ReturnsBadRequest_ForEmptyUsername()
        {
            // Assign
            var user = new UserDeleteDto { Username = "" };

            // Act
            var result = await _controller.DeleteUser(user);

            // Assert
            var actionResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Username is required.", actionResult.Value);
        }

        [Fact]
        public async Task GetUsernames_ReturnsEmptyList_WhenNoUsersExist()
        {
            // Arrange
            _context.Users.RemoveRange(_context.Users);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetUsernames();

            // Assert
            var actionResult = Assert.IsType<ActionResult<List<string>>>(result);
            var usernames = Assert.IsType<List<string>>(actionResult.Value);
            Assert.Empty(usernames);
        }

        [Fact]
        public async Task DeleteUser_ConcurrentDeletes_HandleGracefully()
        {
            // Arrange
            var user1 = new UserDeleteDto { Username = "AdminUser" };
            var user2 = new UserDeleteDto { Username = "AdminUser" };

            // Act
            var task1 = _controller.DeleteUser(user1);
            var task2 = _controller.DeleteUser(user2);
            await Task.WhenAll(task1, task2);

            // Assert
            var result1 = await task1;
            var result2 = await task2;

            Assert.IsType<NoContentResult>(result1);
            Assert.IsType<NotFoundObjectResult>(result2);
        }

        [Fact]
        public async Task DeleteUser_ThrowsException_ForDatabaseError()
        {
            // Arrange
            _context.Dispose(); // Simulate database connection error
            var user = new UserDeleteDto { Username = "AdminUser" };

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateException>(() => _controller.DeleteUser(user));
        }

        [Fact]
        public async Task GetUsernames_ReturnsForbidden_ForNonAdminUser()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "RegularUser")
            };

            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType"));

            // Act
            var result = await _controller.GetUsernames();

            // Assert
            var actionResult = Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task DeleteUser_DeletesAssociatedTodoItems()
        {
            // Assign
            var user = new UserDeleteDto { Username = "AdminUser" };

            // Act
            var result = await _controller.DeleteUser(user);
            var todoItems = _context.TodoItems.Where(t => t.UserId == 1).ToList();

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Null(await _context.Users.FindAsync(1));            
            Assert.Empty(todoItems);
        }

    }
}
