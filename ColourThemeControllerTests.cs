using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Controllers;
using TodoApp.Data;
using TodoApp.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace TodoApp.Tests
{
    public class ColourThemeControllerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly ColourThemeController _controller;
        private readonly string _coloursJsonString;

        public ColourThemeControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _controller = new ColourThemeController(_context);

            _coloursJsonString = "[{\"colourProperty\": \"--button-bgcolour\", \"colourValue\": \"#00796b\"}, " +
                           "{\"colourProperty\": \"--button-hover-bgcolour\", \"colourValue\": \"#004d40\"}, " +
                           "{\"colourProperty\": \"--button-text-colour\", \"colourValue\": \"#ffffff\"}, " +
                           "{\"colourProperty\": \"--input-bgcolour\", \"colourValue\": \"#e0f7fa\"}, " +
                           "{\"colourProperty\": \"--input-border-colour\", \"colourValue\": \"#b2ebf2\"}, " +
                           "{\"colourProperty\": \"--table-header-bgcolour\", \"colourValue\": \"#f1f1db\"}, " +
                           "{\"colourProperty\": \"--table-border-colour\", \"colourValue\": \"#ddd\"}, " +
                           "{\"colourProperty\": \"--modal-bgcolour\", \"colourValue\": \"white\"}, " +
                           "{\"colourProperty\": \"--modal-overlay-bgcolour\", \"colourValue\": \"rgba(0, 0, 0, 0.5)\"}, " +
                           "{\"colourProperty\": \"--loading-spinner-colour\", \"colourValue\": \"#00796b\"}, " +
                           "{\"colourProperty\": \"--logout-button-bgcolour\", \"colourValue\": \"#f56c6c\"}, " +
                           "{\"colourProperty\": \"--portal-switch-button-bgcolour\", \"colourValue\": \"#409EFF\"}]";

            _context.ColourThemes.AddRange(
                new ColourTheme { Id = 1, Name = "Default Theme", Colours = _coloursJsonString, SysDefined = true, IsDefault = true, IsActive = true },
                new ColourTheme { Id = 2, Name = "User Theme", Colours = _coloursJsonString, SysDefined = false, IsDefault = false, UserId = 1, IsActive = true }
            );
            _context.SaveChanges();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"), // Hardcoded UserId
                new Claim(ClaimTypes.Name, "TestUser")
            };

            var httpUser = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType"));

            // Set up the controller context
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
        public async Task GetColourThemes_ReturnsAllThemes()
        {
            // Act
            var result = await _controller.GetColourThemes();

            // Assert
            var actionResult = Assert.IsType<ActionResult<IEnumerable<ColourTheme>>>(result);
            var returnValue = Assert.IsType<List<ColourTheme>>(actionResult.Value);
            Assert.Equal(2, returnValue.Count);
        }

        [Fact]
        public async Task GetColourTheme_ReturnsCorrectTheme()
        {
            // Act
            var result = await _controller.GetColourTheme(2);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ColourTheme>>(result);
            var theme = Assert.IsType<ColourTheme>(actionResult.Value);
            Assert.Equal("User Theme", theme.Name);
            Assert.False(theme.IsDefault);
            Assert.False(theme.SysDefined);
            Assert.NotNull(theme.UserId);
        }

        [Fact]
        public async Task PostColourTheme_CreatesNewTheme()
        {
            // Arrange
            var newTheme = new ColourTheme { Name = "Custom Theme", Colours = "red,green", UserId = 1 };

            // Act
            var result = await _controller.PostColourTheme(newTheme);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var createdTheme = Assert.IsType<ColourTheme>(createdResult.Value);

            Assert.Equal("Custom Theme", createdTheme.Name);
            Assert.Equal("red,green", createdTheme.Colours);
            Assert.Equal(1, createdTheme.UserId);
        }

        [Fact]
        public async Task PutColourTheme_UpdatesTheme()
        {
            // Assign
            int colourThemeId = 2;

            var updatedTheme = new ColourTheme
            {
                Id = colourThemeId,
                Name = "Updated Theme",
                Colours = _coloursJsonString,
                IsActive = true,
                IsDefault = false,
                SysDefined = false,
                UserId = 1
            };

            // Act
            var result = await _controller.PutColourTheme(updatedTheme);
            var theme = await _controller.GetColourTheme(colourThemeId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Equal("Updated Theme", theme.Value.Name);
            Assert.Equal(_coloursJsonString, theme.Value.Colours);
        }

        [Fact]
        public async Task DeleteColourTheme_RemovesTheme()
        {
            // Assign
            int themeId = 2;
            // Act
            var result = await _controller.DeleteColourTheme(themeId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            var theme = await _controller.GetColourTheme(themeId);
            Assert.IsType<NotFoundResult>(theme.Result);
        }

        [Fact]
        public async Task GetColourTheme_ReturnsNotFound_ForInvalidId()
        {
            // Act
            var result = await _controller.GetColourTheme(999);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task PutColourTheme_ReturnsBadRequest_ForMismatchedId()
        {
            // Arrange
            var theme = new ColourTheme { Id = 999, Name = "Mismatched Theme", Colours = "black,white" };

            // Act
            var result = await _controller.PutColourTheme(theme);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteColourTheme_ReturnsNotFound_ForInvalidId()
        {
            // Act
            var result = await _controller.DeleteColourTheme(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
