using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SingleParentSupport2.Controllers;
using SingleParentSupport2.Models;

namespace SingleParentSupport2.UnitTests.Controllers
{
    public class UserManagementControllerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManager;
        private readonly Mock<RoleManager<IdentityRole>> _roleManager;
        private readonly UserManagementController _controller;

        public UserManagementControllerTests()
        {
            _userManager = GetMockUserManager();
            _roleManager = GetMockRoleManager();
            _controller = new UserManagementController(_userManager.Object, _roleManager.Object);
        }

        [Fact]
        public async Task Index_ReturnsViewWithUsers()
        {
            var user = new ApplicationUser { Id = "1", Email = "test@example.com" };
            _userManager.Setup(x => x.Users).Returns(new List<ApplicationUser> { user }.AsQueryable());
            _userManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });

            var result = await _controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<UserManagementViewModel>>(viewResult.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task Details_ReturnsNotFound_WhenIdIsNull()
        {
            var result = await _controller.Details(null);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_ReturnsNotFound_WhenUserNotFound()
        {
            // Arrange
            _userManager.Setup(x => x.FindByIdAsync("nonexistent")).ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _controller.Details("nonexistent");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_ReturnsView_WhenUserExists()
        {
            var user = new ApplicationUser { Id = "1", Email = "test@example.com" };
            _userManager.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(user);
            _userManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });

            var result = await _controller.Details("1");
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<UserManagementViewModel>(viewResult.Model);
        }

        [Fact]
        public async Task Edit_Get_ReturnsNotFound_WhenIdIsNull()
        {
            var result = await _controller.Edit(null);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Get_ReturnsNotFound_WhenUserNotFound()
        {
            // Arrange
            _userManager.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _controller.Edit("user1");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Get_ReturnsViewWithUserEditViewModel_WhenUserExists()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "user1",
                Email = "test@example.com",
                FirstName = "John",
                LastName = "Doe",
                IsVolunteer = true,
                VolunteerRole = "Tutor",
                VolunteerBio = "Helps students"
            };

            var roles = new List<string> { "Admin" };
            var allRoles = new List<IdentityRole> { new IdentityRole("Admin"), new IdentityRole("User") };

            _userManager.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync(user);
            _userManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(roles);
            _roleManager.Setup(r => r.Roles).Returns(allRoles.AsQueryable());

            // Act
            var result = await _controller.Edit("user1") as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<UserEditViewModel>(result.Model);
            Assert.Equal("user1", model.Id);
            Assert.Equal("John", model.FirstName);
            Assert.Contains("Admin", model.CurrentRoles);
            Assert.Contains("User", model.AllRoles);
        }

        [Fact]
        public async Task Edit_Post_UpdatesUser_AndRedirects()
        {
            var model = new UserEditViewModel { Id = "1", Email = "new@example.com", SelectedRoles = new List<string> { "User" } };
            var user = new ApplicationUser { Id = "1", Email = "old@example.com" };

            _userManager.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(user);
            _userManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });
            _userManager.Setup(x => x.RemoveFromRolesAsync(user, It.IsAny<string[]>())).ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(x => x.AddToRolesAsync(user, It.IsAny<string[]>())).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.Edit("1", model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
        }

        [Fact]
        public async Task Edit_Post_ReturnsNotFound_WhenIdMismatch()
        {
            // Arrange
            var model = new UserEditViewModel { Id = "user1" };

            // Act
            var result = await _controller.Edit("wrongId", model);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Post_ReturnsNotFound_WhenUserNotFound()
        {
            // Arrange
            var model = new UserEditViewModel { Id = "user1" };
            _userManager.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _controller.Edit("user1", model);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Post_ReturnsView_WhenUpdateFails()
        {
            // Arrange
            var model = new UserEditViewModel
            {
                Id = "user1",
                FirstName = "John",
                LastName = "Doe",
                Email = "test@example.com"
            };

            var user = new ApplicationUser { Id = "user1", Email = "test@example.com" };

            _userManager.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync(user);
            _userManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Error" }));

            // Act
            var result = await _controller.Edit("user1", model) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Same(model, result.Model);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains(_controller.ModelState[string.Empty].Errors, e => e.ErrorMessage == "Error");
        }

        [Fact]
        public async Task Edit_Post_ReturnsViewWithRoles_WhenModelStateInvalid()
        {
            // Arrange
            _controller.ModelState.AddModelError("key", "error");

            var model = new UserEditViewModel { Id = "user1" };

            var allRoles = new List<IdentityRole> { new IdentityRole("Admin"), new IdentityRole("User") };
            _roleManager.Setup(r => r.Roles).Returns(allRoles.AsQueryable());

            // Act
            var result = await _controller.Edit("user1", model) as ViewResult;

            // Assert
            Assert.NotNull(result);
            var returnedModel = Assert.IsType<UserEditViewModel>(result.Model);
            Assert.Contains("Admin", returnedModel.AllRoles);
            Assert.Contains("User", returnedModel.AllRoles);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenIdIsNull()
        {
            // Act
            var result = await _controller.Delete(null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenUserNotFound()
        {
            // Arrange
            _userManager.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _controller.Delete("user1");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsView_WithUserManagementViewModel()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "user1",
                Email = "test@example.com",
                FirstName = "John",
                LastName = "Doe",
                JoinDate = new DateTime(2020, 1, 1)
            };

            var roles = new List<string> { "Admin", "User" };

            _userManager.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync(user);
            _userManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(roles);

            // Act
            var result = await _controller.Delete("user1") as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<UserManagementViewModel>(result.Model);
            Assert.Equal("user1", model.Id);
            Assert.Equal("test@example.com", model.Email);
            Assert.Equal("John", model.FirstName);
            Assert.Equal("Doe", model.LastName);
            Assert.Equal(new DateTime(2020, 1, 1), model.JoinDate);
            Assert.Equal(roles, model.Roles);
        }

        [Fact]
        public async Task DeleteConfirmed_DeletesUser_AndRedirects()
        {
            var user = new ApplicationUser { Id = "1" };
            _userManager.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(user);
            _userManager.Setup(x => x.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.DeleteConfirmed("1");
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
        }

        [Fact]
        public async Task DeleteConfirmed_ReturnsNotFound_WhenUserNotFound()
        {
            // Arrange
            _userManager.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _controller.DeleteConfirmed("user1");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteConfirmed_RedirectsToDelete_WhenDeleteFails()
        {
            // Arrange
            var user = new ApplicationUser { Id = "user1", Email = "user@example.com" };
            _userManager.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync(user);

            var identityResult = IdentityResult.Failed(new IdentityError { Description = "Delete failed" });
            _userManager.Setup(x => x.DeleteAsync(user)).ReturnsAsync(identityResult);

            // Act
            var result = await _controller.DeleteConfirmed("user1") as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Delete", result.ActionName);
            Assert.Equal("user1", result.RouteValues["id"]);
        }

        [Fact]
        public async Task DeleteConfirmed_RedirectsToIndex_WhenDeleteSucceeds()
        {
            // Arrange
            var user = new ApplicationUser { Id = "user1", Email = "user@example.com" };
            _userManager.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync(user);
            _userManager.Setup(x => x.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.DeleteConfirmed("user1") as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
        }

        [Fact]
        public async Task ChangePassword_ReturnsNotFound_WhenIdIsNull()
        {
            // Act
            var result = await _controller.ChangePassword(id: null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task ChangePassword_ReturnsNotFound_WhenUserNotFound()
        {
            // Arrange
            _userManager.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _controller.ChangePassword("user1");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task ChangePassword_ReturnsView_WithCorrectModel()
        {
            // Arrange
            var user = new ApplicationUser { Id = "user1", Email = "test@example.com" };
            _userManager.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync(user);

            // Act
            var result = await _controller.ChangePassword("user1") as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<ChangePasswordViewModel>(result.Model);
            Assert.Equal("user1", model.UserId);
            Assert.Equal("test@example.com", model.Email);
        }

        [Fact]
        public void Create_Get_ReturnsView()
        {
            var result = _controller.Create();
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Create_Post_CreatesUser_AndRedirects()
        {
            var model = new CreateUserViewModel { Email = "user@example.com", FirstName = "First", LastName = "Last", Password = "Password123!", Role = "User" };
            _roleManager.Setup(x => x.RoleExistsAsync("User")).ReturnsAsync(false);
            _userManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), model.Password)).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.Create(model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
        }

        [Fact]
        public async Task Create_Post_ReturnsView_WhenModelStateIsInvalid()
        {
            // Arrange
            var model = new CreateUserViewModel
            {
                Email = "test@example.com",
                FirstName = "First",
                LastName = "Last",
                Password = "Password123!",
                IsVolunteer = true,
                Role = "User"
            };

            _controller.ModelState.AddModelError("Error", "Invalid model state");

            // Act
            var result = await _controller.Create(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
        }

        [Fact]
        public async Task Create_Post_ReturnsView_WhenUserCreationFails()
        {
            // Arrange
            var model = new CreateUserViewModel
            {
                Email = "test@example.com",
                FirstName = "First",
                LastName = "Last",
                Password = "Password123!",
                IsVolunteer = true,
                Role = "User"
            };

            var identityErrors = new List<IdentityError>
            {
                new IdentityError { Description = "User creation failed" }
            };
            var failedResult = IdentityResult.Failed(identityErrors.ToArray());

            _userManager.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), model.Password))
                        .ReturnsAsync(failedResult);

            // Act
            var result = await _controller.Create(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains(_controller.ModelState, ms => ms.Value.Errors.Any(e => e.ErrorMessage == "User creation failed"));
        }

        [Fact]
        public async Task ChangePassword_Post_ValidModel_ChangesPassword()
        {
            var model = new ChangePasswordViewModel { UserId = "1", NewPassword = "NewPass123!" };
            var user = new ApplicationUser { Id = "1", Email = "user@example.com" };

            _userManager.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(user);
            _userManager.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("token123");
            _userManager.Setup(x => x.ResetPasswordAsync(user, "token123", model.NewPassword)).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.ChangePassword(model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
        }

        [Fact]
        public async Task ChangePassword_Post_ReturnsView_WhenModelStateIsInvalid()
        {
            // Arrange
            var model = new ChangePasswordViewModel { UserId = "someId", NewPassword = "NewPass123!" };
            _controller.ModelState.AddModelError("Error", "ModelState invalid");

            // Act
            var result = await _controller.ChangePassword(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
        }

        [Fact]
        public async Task ChangePassword_Post_ReturnsNotFound_WhenUserIsNull()
        {
            // Arrange
            var model = new ChangePasswordViewModel { UserId = "nonexistent", NewPassword = "NewPass123!" };
            _userManager.Setup(u => u.FindByIdAsync(model.UserId)).ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _controller.ChangePassword(model);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task ChangePassword_Post_ReturnsView_WhenResetPasswordFails()
        {
            // Arrange
            var model = new ChangePasswordViewModel { UserId = "user1", NewPassword = "NewPass123!" };
            var user = new ApplicationUser { Id = model.UserId, Email = "test@example.com" };
            _userManager.Setup(u => u.FindByIdAsync(model.UserId)).ReturnsAsync(user);
            _userManager.Setup(u => u.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("token");

            var identityErrors = new List<IdentityError>
            {
                new IdentityError { Description = "Password reset failed" }
            };
            var failedResult = IdentityResult.Failed(identityErrors.ToArray());
            _userManager.Setup(u => u.ResetPasswordAsync(user, "token", model.NewPassword))
                        .ReturnsAsync(failedResult);

            // Act
            var result = await _controller.ChangePassword(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains(_controller.ModelState, ms => ms.Value.Errors.Any(e => e.ErrorMessage == "Password reset failed"));
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
