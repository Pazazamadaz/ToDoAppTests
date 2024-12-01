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
        private readonly List<Colour> _colours;

        public ColourThemeControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _controller = new ColourThemeController(_context);

            _colours = new List<Colour>
            {
                new Colour { ColourProperty = "--button-bgcolour", ColourValue = "#00796b", ColourThemeId = 1 },
                new Colour { ColourProperty = "--button-hover-bgcolour", ColourValue = "#004d40", ColourThemeId = 1 },
                new Colour { ColourProperty = "--button-text-colour", ColourValue = "#ffffff", ColourThemeId = 1 },
                new Colour { ColourProperty = "--input-bgcolour", ColourValue = "#e0f7fa", ColourThemeId = 1 },
                new Colour { ColourProperty = "--input-border-colour", ColourValue = "#b2ebf2", ColourThemeId = 1 },
                new Colour { ColourProperty = "--table-header-bgcolour", ColourValue = "#f1f1db", ColourThemeId = 1 },
                new Colour { ColourProperty = "--table-border-colour", ColourValue = "#ddd", ColourThemeId = 1 },
                new Colour { ColourProperty = "--modal-bgcolour", ColourValue = "white", ColourThemeId = 1 },
                new Colour { ColourProperty = "--modal-overlay-bgcolour", ColourValue = "rgba(0, 0, 0, 0.5)", ColourThemeId = 1 },
                new Colour { ColourProperty = "--loading-spinner-colour", ColourValue = "#00796b", ColourThemeId = 1 },
                new Colour { ColourProperty = "--logout-button-bgcolour", ColourValue = "#f56c6c", ColourThemeId = 1 },
                new Colour { ColourProperty = "--portal-switch-button-bgcolour", ColourValue = "#409EFF", ColourThemeId = 1 }
            };


            // Update ColourTheme objects to use List<Colour>
            _context.ColourThemes.AddRange(
                new ColourTheme { Id = 1, Name = "Default Theme", Colours = _colours, SystemDefined = true, IsDefault = true, IsActive = true },
                new ColourTheme { Id = 2, Name = "User Theme", Colours = _colours, SystemDefined = false, IsDefault = false, UserId = 1, IsActive = true }
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
            Assert.False(theme.SystemDefined);
            Assert.NotNull(theme.UserId);
        }

        [Fact]
        public async Task PostColourTheme_CreatesNewTheme()
        {
            // Arrange
            var newTheme = new ColourTheme { Name = "Custom Theme", Colours = _colours, UserId = 1 };

            // Act
            var result = await _controller.PostColourTheme(newTheme);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var createdTheme = Assert.IsType<ColourTheme>(createdResult.Value);

            Assert.Equal("Custom Theme", createdTheme.Name);
            Assert.Equal(_colours, createdTheme.Colours);
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
                Colours = _colours,
                IsActive = true,
                IsDefault = false,
                SystemDefined = false,
                UserId = 1
            };

            // Act
            var result = await _controller.PutColourTheme(updatedTheme);
            var theme = await _controller.GetColourTheme(colourThemeId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Equal("Updated Theme", theme.Value.Name);
            Assert.Equal(_colours, theme.Value.Colours);
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
            var theme = new ColourTheme { Id = 999, Name = "Mismatched Theme", Colours = _colours };

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
