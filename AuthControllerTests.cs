using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using TodoApp.Authentication;
using TodoApp.Dtos;
using TodoApp.Models;
using TodoApp.Services;
using Xunit;

namespace TodoApp.Tests
{
    public class AuthControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly AuthController _authController;

        public AuthControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockConfiguration = new Mock<IConfiguration>();

            // Mock JWT settings in the configuration
            _mockConfiguration.Setup(x => x.GetSection("JwtSettings")["Issuer"]).Returns("testIssuer");
            _mockConfiguration.Setup(x => x.GetSection("JwtSettings")["Audience"]).Returns("testAudience");
            _mockConfiguration.Setup(x => x.GetSection("JwtSettings")["SecretKey"]).Returns("testSecretKey");

            _authController = new AuthController(_mockUserService.Object, _mockConfiguration.Object);
        }

        [Fact]
        public async Task Register_ValidUser_ReturnsOk()
        {
            // Arrange
            var registerDto = new UserRegisterDto
            {
                Username = "testuser",
                Password = "Test@123"
            };

            _mockUserService.Setup(x => x.Register(registerDto))
                .ReturnsAsync(new User
                {
                    Username = registerDto.Username
                }); // Simulate successful registration

            // Act
            var result = await _authController.Register(registerDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("User registered successfully", okResult.Value);
        }

        [Fact]
        public async Task Register_InvalidUser_ReturnsBadRequest()
        {
            // Arrange
            var registerDto = new UserRegisterDto
            {
                Username = "testuser",
                Password = "" // Invalid password
            };

            _mockUserService.Setup(x => x.Register(registerDto))
                .ReturnsAsync((User)null); // Simulate registration failure

            // Act
            var result = await _authController.Register(registerDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsOkWithToken()
        {
            // Arrange
            var loginDto = new UserLoginDto
            {
                Username = "testuser",
                Password = "Test@123"
            };

            var token = "fake-jwt-token";

            // Simulate successful authentication by returning a Task<string>
            _mockUserService.Setup(x => x.Authenticate(loginDto.Username, loginDto.Password))
                .ReturnsAsync(token);  // Use ReturnsAsync to return a Task<string>

            // Act
            var result = await _authController.Login(loginDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);

            // Retrieve the "Token" property from the anonymous type returned by the controller
            var value = okResult.Value.GetType().GetProperty("Token").GetValue(okResult.Value, null);

            // Assert the token value is correct
            Assert.Equal(token, value);
        }


        [Fact]
        public async Task Login_InvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var loginDto = new UserLoginDto
            {
                Username = "wronguser",
                Password = "wrongpassword"
            };

            _mockUserService.Setup(x => x.Authenticate(loginDto.Username, loginDto.Password))
                .ReturnsAsync((string)null); // Simulate failed authentication

            // Act
            var result = await _authController.Login(loginDto);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Register_InvalidUsername_ReturnsBadRequest()
        {
            // Arrange
            var invalidUsernameDto = new UserRegisterDto
            {
                Username = "test@user!", // Invalid characters
                Password = "Test@123"
            };

            // Since this is a validation error, the service might not even be called.
            // Optionally set up a mock to ensure no registration occurs.
            _mockUserService.Setup(x => x.Register(It.IsAny<UserRegisterDto>()))
                .ReturnsAsync((User)null);

            // Act
            var result = await _authController.Register(invalidUsernameDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid username format", badRequestResult.Value);
        }

    }
}
