using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SingleParentSupport2.Controllers;
using SingleParentSupport2.Models;

namespace SingleParentSupport2.UnitTests.Controllers
{
    public class UserApiControllerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<RoleManager<IdentityRole>> _mockRoleManager;
        private readonly UserApiController _controller;

        public UserApiControllerTests()
        {
            _mockUserManager = GetMockUserManager();
            _mockRoleManager = GetMockRoleManager();
            _controller = new UserApiController(_mockUserManager.Object, _mockRoleManager.Object);
        }

        [Fact]
        public async Task GetUsers_ReturnsUserList()
        {
            var users = new List<ApplicationUser>
            {
                new() { Id = "1", Email = "test@example.com", FirstName = "First", LastName = "Last", JoinDate = DateTime.Now, IsVolunteer = true }
            }.AsQueryable();

            _mockUserManager.Setup(u => u.Users).Returns(users);
            _mockUserManager.Setup(u => u.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(["User"]);

            var result = await _controller.GetUsers() as OkObjectResult;

            Assert.NotNull(result);
            var model = Assert.IsType<List<UserManagementViewModel>>(result.Value);
            Assert.Single(model);
        }

        [Fact]
        public async Task GetUser_ReturnsUser_WhenExists()
        {
            var user = new ApplicationUser { Id = "1", Email = "user@test.com" };
            _mockUserManager.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
            _mockUserManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["Admin"]);

            var result = await _controller.GetUser("1") as OkObjectResult;

            Assert.NotNull(result);
            Assert.IsType<UserManagementViewModel>(result.Value);
        }

        [Fact]
        public async Task GetUser_ReturnsNotFound_WhenUserDoesNotExist()
        {
            _mockUserManager.Setup(m => m.FindByIdAsync("1")).ReturnsAsync((ApplicationUser)null);

            var result = await _controller.GetUser("1");

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateUser_ReturnsBadRequest_WhenIdMismatch()
        {
            var result = await _controller.UpdateUser("1", new UserEditViewModel { Id = "2" });

            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task UpdateUser_ReturnsNotFound_WhenUserMissing()
        {
            _mockUserManager.Setup(m => m.FindByIdAsync("1")).ReturnsAsync((ApplicationUser)null);

            var result = await _controller.UpdateUser("1", new UserEditViewModel { Id = "1" });

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateUser_ReturnsNoContent_WhenSuccessful()
        {
            var user = new ApplicationUser { Id = "1", Email = "test" };

            _mockUserManager.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
            _mockUserManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string>());
            _mockUserManager.Setup(m => m.RemoveFromRolesAsync(user, It.IsAny<string[]>())).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.AddToRolesAsync(user, It.IsAny<string[]>())).ReturnsAsync(IdentityResult.Success);

            var model = new UserEditViewModel { Id = "1", SelectedRoles = new List<string>() };

            var result = await _controller.UpdateUser("1", model);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task UpdateUser_ReturnsBadRequest_WhenUpdateFails()
        {
            var user = new ApplicationUser { Id = "1" };
            _mockUserManager.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
            _mockUserManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Failed" }));

            var result = await _controller.UpdateUser("1", new UserEditViewModel { Id = "1" });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateUser_ValidModel_UpdatesUserAndRoles()
        {
            // Arrange
            var userId = "1";
            var existingUser = new ApplicationUser
            {
                Id = userId,
                FirstName = "OldFirst",
                LastName = "OldLast",
                Email = "old@example.com",
                IsVolunteer = false
            };

            var model = new UserEditViewModel
            {
                Id = userId,
                FirstName = "NewFirst",
                LastName = "NewLast",
                Email = "new@example.com",
                IsVolunteer = true,
                VolunteerRole = "Mentor",
                VolunteerBio = "Bio",
                SelectedRoles = new List<string> { "User", "Manager" }
            };

            var currentRoles = new List<string> { "Admin", "User" }; // Admin should be removed, Manager should be added

            _mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(existingUser);
            _mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.GetRolesAsync(existingUser)).ReturnsAsync(currentRoles);
            _mockUserManager.Setup(m => m.RemoveFromRolesAsync(existingUser, It.IsAny<IEnumerable<string>>()))
                            .ReturnsAsync(IdentityResult.Success)
                            .Verifiable();
            _mockUserManager.Setup(m => m.AddToRolesAsync(existingUser, It.IsAny<IEnumerable<string>>()))
                            .ReturnsAsync(IdentityResult.Success)
                            .Verifiable();

            // Act
            var result = await _controller.UpdateUser(userId, model);

            // Assert
            Assert.IsType<NoContentResult>(result);

            // Verify roles updated
            _mockUserManager.Verify(m => m.RemoveFromRolesAsync(existingUser, It.Is<IEnumerable<string>>(r => r.Contains("Admin"))), Times.Once);
            _mockUserManager.Verify(m => m.AddToRolesAsync(existingUser, It.Is<IEnumerable<string>>(r => r.Contains("Manager"))), Times.Once);
        }


        [Fact]
        public async Task DeleteUser_ReturnsNoContent_WhenSuccess()
        {
            var user = new ApplicationUser { Id = "1" };
            _mockUserManager.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
            _mockUserManager.Setup(m => m.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.DeleteUser("1");

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteUser_ReturnsNotFound_WhenUserMissing()
        {
            _mockUserManager.Setup(m => m.FindByIdAsync("1")).ReturnsAsync((ApplicationUser)null);

            var result = await _controller.DeleteUser("1");

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteUser_ReturnsBadRequest_WhenDeleteFails()
        {
            var user = new ApplicationUser { Id = "1" };
            _mockUserManager.Setup(m => m.FindByIdAsync("1")).ReturnsAsync(user);
            _mockUserManager.Setup(m => m.DeleteAsync(user)).ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Delete Failed" }));

            var result = await _controller.DeleteUser("1");

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public void GetRoles_ReturnsRoleList()
        {
            var roles = new List<IdentityRole> { new IdentityRole("Admin"), new IdentityRole("User") }.AsQueryable();
            _mockRoleManager.Setup(r => r.Roles).Returns(roles);

            var result = _controller.GetRoles() as OkObjectResult;

            Assert.NotNull(result);
            var roleNames = Assert.IsAssignableFrom<IEnumerable<string>>(result.Value);
            Assert.Contains("Admin", roleNames);
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

        private static Mock<RoleManager<IdentityRole>> GetMockRoleManager()
        {
            var store = new Mock<IRoleStore<IdentityRole>>();
            return new Mock<RoleManager<IdentityRole>>(
                store.Object,
                new List<IRoleValidator<IdentityRole>>(),
                new Mock<ILookupNormalizer>().Object,
                new Mock<IdentityErrorDescriber>().Object,
                new Mock<ILogger<RoleManager<IdentityRole>>>().Object
            );
        }
    }
}