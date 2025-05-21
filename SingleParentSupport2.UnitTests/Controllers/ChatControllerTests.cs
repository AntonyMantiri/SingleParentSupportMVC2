using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SingleParentSupport2.Controllers;
using SingleParentSupport2.Models;

namespace SingleParentSupport2.UnitTests.Controllers
{
    public class ChatControllerTests
    {
        private readonly AppDbContext _dbContext;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly ChatController _controller;
        private readonly ClaimsPrincipal _user;

        public ChatControllerTests()
        {
            _dbContext = GetInMemoryDbContext();
            _mockUserManager = GetMockUserManager();

            var user = new ApplicationUser { Id = "user123", UserName = "testuser" };
            _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(user.Id);

            _dbContext.Users.Add(user);
            _dbContext.Users.Add(new ApplicationUser { Id = "partner1", FirstName = "Alice", LastName = "Smith", UserName = "alice" });
            _dbContext.SaveChanges();

            _user = new ClaimsPrincipal(new ClaimsIdentity(
            [
            new Claim(ClaimTypes.NameIdentifier, user.Id)
            ]));

            _controller = new ChatController(_dbContext, _mockUserManager.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = _user }
                }
            };
        }

        [Fact]
        public async Task Index_ReturnsChatPageViewModel()
        {
            // Arrange
            _dbContext.ChatLogs.Add(new ChatLog
            {
                SenderId = "user123",
                ReceiverId = "partner1",
                Content = "Hi!",
                Timestamp = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.Index(null) as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = result.Model as ChatPageViewModel;
            Assert.NotNull(model);
            Assert.NotEmpty(model.ChatRooms);
            Assert.NotEmpty(model.Messages);
        }

        [Fact]
        public async Task Index_NoMessages_ReturnsEmptyChatRooms()
        {
            // Act
            var result = await _controller.Index(null) as ViewResult;

            // Assert
            var model = result.Model as ChatPageViewModel;
            Assert.NotNull(model);
            Assert.Empty(model.ChatRooms);
            Assert.Empty(model.Messages);
        }

        [Fact]
        public async Task SendMessage_ValidModel_ReturnsSuccessJson()
        {
            // Arrange
            var model = new ChatLog { ReceiverId = "partner1", Content = "Hello" };

            // Act
            var result = await _controller.SendMessage(model) as JsonResult;

            // Assert
            Assert.NotNull(result);

            var jsonString = JsonSerializer.Serialize(result.Value);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var message = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString, options);

            Assert.Equal("True", message["success"].ToString());
            Assert.Equal("", message["avatarUrl"].ToString());
        }

        [Fact]
        public async Task SendMessage_InvalidModel_ReturnsError()
        {
            // Arrange
            _controller.ModelState.AddModelError("Content", "Required");
            var model = new ChatLog { ReceiverId = "partner1" };

            // Act
            var result = await _controller.SendMessage(model) as JsonResult;

            // Assert
            Assert.NotNull(result);

            var jsonString = JsonSerializer.Serialize(result.Value);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var message = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString, options);

            Assert.Equal("False", message["success"].ToString());
            Assert.Equal("Model invalid.", message["message"].ToString());
        }

        [Fact]
        public async Task SendMessage_ExceptionThrown_ReturnsServerErrorJson()
        {
            // Arrange
            var model = new ChatLog { ReceiverId = "partner1", Content = "Test message" };

            // Make ModelState valid
            _controller.ModelState.Clear();

            // Set up GetUserId to throw exception
            _mockUserManager.Setup(c => c.GetUserId(It.IsAny<ClaimsPrincipal>()))
                        .Throws(new Exception("Fake exception"));

            // Act
            var result = await _controller.SendMessage(model) as JsonResult;

            // Assert
            Assert.NotNull(result);

            var jsonString = JsonSerializer.Serialize(result.Value);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var error = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString, options);

            Assert.Equal("False", error["success"].ToString());
            Assert.Equal("Server error: Fake exception", error["message"].ToString());
        }

        [Fact]
        public async Task SendMessage_UserNotAuthenticated_ReturnsFailureJson()
        {
            // Arrange
            _mockUserManager.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
                       .Returns<string>(null);

            var model = new ChatLog
            {
                ReceiverId = "anyReceiverId",
                Content = "Hello"
            };

            // Act
            var result = await _controller.SendMessage(model) as JsonResult;

            // Assert
            Assert.NotNull(result);

            var jsonString = JsonSerializer.Serialize(result.Value);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var error = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString, options);

            Assert.Equal("False", error["success"].ToString());
            Assert.Equal("User not authenticated.", error["message"].ToString());
        }

        [Fact]
        public async Task GetMessages_ReturnsMessagesJson()
        {
            // Arrange
            _dbContext.ChatLogs.Add(new ChatLog
            {
                SenderId = "partner1",
                ReceiverId = "user123",
                Content = "Hey!",
                Timestamp = DateTime.UtcNow,
                IsRead = false
            });
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.GetMessages("partner1") as JsonResult;

            // Assert
            var messages = result.Value as IEnumerable<dynamic>;
            Assert.NotNull(messages);
            Assert.Single(messages);
        }

        private static AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private static Mock<UserManager<ApplicationUser>> GetMockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(
                store.Object,
                new Mock<IOptions<IdentityOptions>>().Object,
                new Mock<IPasswordHasher<ApplicationUser>>().Object,
                Array.Empty<IUserValidator<ApplicationUser>>(),
                Array.Empty<IPasswordValidator<ApplicationUser>>(),
                new Mock<ILookupNormalizer>().Object,
                new Mock<IdentityErrorDescriber>().Object,
                new Mock<IServiceProvider>().Object,
                new Mock<ILogger<UserManager<ApplicationUser>>>().Object
            );
        }
    }
}
