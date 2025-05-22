using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SingleParentSupport2.Controllers;
using SingleParentSupport2.Models;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace SingleParentSupport2.UnitTests.Controllers
{
    public class AccountControllerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly AccountController _controller;
        private readonly Mock<ILogger<AccountController>> _mockLogger;

        public AccountControllerTests()
        {
            _mockUserManager = GetMockUserManager();
            var signInManager = GetSignInManager();
            _mockLogger = new Mock<ILogger<AccountController>>();
            _controller = new AccountController(signInManager.Object, _mockUserManager.Object, _mockLogger.Object);
        }

        [Fact]
        public void Login_Get_ReturnsViewWithReturnUrl()
        {
            // Act
            var result = _controller.Login("/home") as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("/home", _controller.ViewData["ReturnUrl"]);
        }

        [Fact]
        public async Task Login_Post_InvalidModel_ReturnsViewWithModel()
        {
            // Arrange
            _controller.ModelState.AddModelError("Email", "Required");

            // Act
            var model = new LoginViewModel();
            var result = await _controller.Login(model) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(model, result.Model);
        }

        [Fact]
        public async Task Login_Post_ValidCredentials_ReturnsRedirect()
        {
            // Arrange
            var signInManager = GetSignInManager();
            signInManager.Setup(s => s.PasswordSignInAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(SignInResult.Success);

            var controller = new AccountController(signInManager.Object, null, _mockLogger.Object);

            var model = new LoginViewModel
            {
                Email = "test@test.com",
                Password = "123",
                RememberMe = false,
                ReturnUrl = "/dashboard"
            };

            // Act
            var result = await controller.Login(model) as LocalRedirectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("/dashboard", result.Url);
        }


        [Fact]
        public async Task Login_Post_TwoFactorRequired_ReturnsRedirectToPage()
        {
            // Arrange
            var signInManager = GetSignInManager();
            signInManager.Setup(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                         .ReturnsAsync(SignInResult.TwoFactorRequired);

            var controller = new AccountController(signInManager.Object, null, _mockLogger.Object);
            var model = new LoginViewModel { Email = "test@test.com", Password = "123", RememberMe = true };

            // Act
            var result = await controller.Login(model);

            // Assert
            var redirect = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("./LoginWith2fa", redirect.PageName);
        }

        [Fact]
        public async Task Login_Post_IsLockedOut_ReturnsLockout()
        {
            // Arrange
            var signInManager = GetSignInManager();
            signInManager.Setup(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), false, false))
                         .ReturnsAsync(SignInResult.LockedOut);

            var controller = new AccountController(signInManager.Object, null, _mockLogger.Object);
            var model = new LoginViewModel { Email = "locked@test.com", Password = "123" };

            // Act
            var result = await controller.Login(model);

            // Assert
            var redirect = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("./Lockout", redirect.PageName);
        }

        [Fact]
        public async Task Login_Post_InvalidCredentials_ReturnsViewWithError()
        {
            // Arrange
            var signInManager = GetSignInManager();
            signInManager.Setup(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), false, false))
                         .ReturnsAsync(SignInResult.Failed);

            var controller = new AccountController(signInManager.Object, null, _mockLogger.Object);
            var model = new LoginViewModel { Email = "fail@test.com", Password = "123" };

            // Act
            var result = await controller.Login(model) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.Equal(model, result.Model);
        }

        [Fact]
        public void ExternalLogin_ReturnsChallengeResult()
        {
            // Arrange
            var signInManager = GetSignInManager();
            signInManager.Setup(s => s.ConfigureExternalAuthenticationProperties("Google", It.IsAny<string>(), null))
                         .Returns(new AuthenticationProperties());

            var urlHelper = new Mock<IUrlHelper>(MockBehavior.Strict);

            urlHelper.Setup(u => u.Action(
                It.Is<UrlActionContext>(ctx =>
                    ctx.Action == "ExternalLoginCallback" &&
                    ctx.Controller == "Account"))
                ).Returns("/external-callback");

            var controller = new AccountController(signInManager.Object, null, _mockLogger.Object)
            {
                Url = urlHelper.Object
            };

            // Act
            var result = controller.ExternalLogin("Google", "/dashboard") as ChallengeResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Google", result.AuthenticationSchemes.First());

        }

        [Fact]
        public void Register_Get_ReturnsView()
        {
            // Act
            var result = _controller.Register();

            // Assert
            Assert.IsType<ViewResult>(result);
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

        private Mock<SignInManager<ApplicationUser>> GetSignInManager()
        {
            return new Mock<SignInManager<ApplicationUser>>(
                _mockUserManager.Object,
                new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>().Object,
                new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>().Object,
                new Mock<IOptions<IdentityOptions>>().Object,
                new Mock<ILogger<SignInManager<ApplicationUser>>>().Object,
                new Mock<IAuthenticationSchemeProvider>().Object,
                new Mock<IUserConfirmation<ApplicationUser>>().Object
            );
        }
    }
}
