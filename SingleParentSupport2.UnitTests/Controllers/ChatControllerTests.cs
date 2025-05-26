using System.Security.Claims;
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

        public ChatControllerTests()
        {
            _dbContext = GetInMemoryDbContext();
            _mockUserManager = GetMockUserManager();
            _controller = new ChatController(_mockUserManager.Object, _dbContext);
        }

        [Fact]
        public async Task Index_ForRegularUser_ShouldReturnVolunteersAndAdmins()
        {
            // Arrange
            var currentUser = CreateUser("1", "regularUser");
            var volunteers = new List<ApplicationUser>
            {
                CreateUser("2", "johndoe"),
                CreateUser("3", "michaelbrown")
            };

            var admins = new List<ApplicationUser>
            {
                CreateUser("4", "sarahjohnson")
            };

            _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(currentUser);

            _mockUserManager.Setup(m => m.GetUsersInRoleAsync("Volunteer"))
                .ReturnsAsync(volunteers);

            _mockUserManager.Setup(m => m.GetUsersInRoleAsync("Admin"))
                .ReturnsAsync(admins);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = GetUserPrincipal("1", "User")
                }
            };

            // Act
            var result = await _controller.Index() as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(currentUser, result.ViewData["CurrentUser"]);
            var chatPartners = Assert.IsAssignableFrom<List<ApplicationUser>>(result.ViewData["ChatPartners"]);
            Assert.Equal(3, chatPartners.Count);
            Assert.DoesNotContain(chatPartners, u => u.Id == "1");
        }

        [Fact]
        public async Task Index_ForVolunteer_ShouldReturnUsers()
        {
            // Arrange
            var currentUser = CreateUser("10", "volunteerUser");
            var users = new List<ApplicationUser>
            {
                CreateUser("11", "user1"),
                CreateUser("12", "user2")
            };

            _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(currentUser);

            _mockUserManager.Setup(m => m.GetUsersInRoleAsync("User"))
                .ReturnsAsync(users);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = GetUserPrincipal("10", "Volunteer")
                }
            };

            // Act
            var result = await _controller.Index() as ViewResult;

            // Assert
            Assert.NotNull(result);
            var chatPartners = Assert.IsAssignableFrom<List<ApplicationUser>>(result.ViewData["ChatPartners"]);
            Assert.Equal(2, chatPartners.Count);
        }

        [Fact]
        public async Task Index_RemovesCurrentUserFromChatPartners()
        {
            // Arrange
            var currentUser = CreateUser("5", "adminUser");
            var users = new List<ApplicationUser>
            {
                CreateUser("5", "adminUser"),
                CreateUser("6", "user6")
            };

            _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(currentUser);

            _mockUserManager.Setup(m => m.GetUsersInRoleAsync("User"))
                .ReturnsAsync(users);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = GetUserPrincipal("5", "Admin")
                }
            };

            // Act
            var result = await _controller.Index() as ViewResult;

            // Assert
            var chatPartners = Assert.IsAssignableFrom<List<ApplicationUser>>(result.ViewData["ChatPartners"]);
            Assert.Single(chatPartners);
            Assert.DoesNotContain(chatPartners, u => u.Id == "5");
        }

        [Fact]
        public async Task Index_SetsVolunteerPhotosCorrectly()
        {
            // Arrange
            var currentUser = CreateUser("9", "testUser");
            var volunteers = new List<ApplicationUser>();

            var admins = new List<ApplicationUser>();

            _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(currentUser);

            _mockUserManager.Setup(m => m.GetUsersInRoleAsync("Volunteer"))
                .ReturnsAsync(volunteers);

            _mockUserManager.Setup(m => m.GetUsersInRoleAsync("Admin"))
                .ReturnsAsync(admins);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = GetUserPrincipal("9", "User")
                }
            };

            // Act
            var result = await _controller.Index() as ViewResult;

            // Assert
            var volunteerPhotos = Assert.IsType<Dictionary<string, string>>(result.ViewData["VolunteerPhotos"]);
            Assert.True(volunteerPhotos.ContainsKey("johndoe"));
            Assert.Equal("/images/johndoe.jpeg", volunteerPhotos["johndoe"]);
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

        private static ApplicationUser CreateUser(string id, string username)
        {
            return new ApplicationUser
            {
                Id = id,
                UserName = username
            };
        }

        private static ClaimsPrincipal GetUserPrincipal(string userId, string role)
        {
            return new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role)
            }, "mock"));
        }
    }
}
