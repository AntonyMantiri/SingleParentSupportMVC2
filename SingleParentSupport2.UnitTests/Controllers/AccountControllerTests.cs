using System.Security.Claims;
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

        [Fact]
        public async Task Register_Post_ValidModel_RedirectsToHome()
        {
            // Arrange
            var signInManager = GetSignInManager();

            _mockUserManager.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), "password"))
                       .ReturnsAsync(IdentityResult.Success);

            _mockUserManager.Setup(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"))
                       .ReturnsAsync(IdentityResult.Success);

            signInManager.Setup(s => s.SignInAsync(It.IsAny<ApplicationUser>(), false, null))
                         .Returns(Task.CompletedTask);

            var controller = new AccountController(signInManager.Object, _mockUserManager.Object, null);
            var model = new RegisterViewModel { Email = "test@test.com", Password = "password", FirstName = "Test", LastName = "User" };

            // Act
            var result = await controller.Register(model);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Home", redirect.ControllerName);
        }

        [Fact]
        public async Task Register_Post_ModelInvalid_ReturnsView()
        {
            // Arrange
            _controller.ModelState.AddModelError("Email", "Required");

            var model = new RegisterViewModel();

            // Act
            var result = await _controller.Register(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
        }

        [Fact]
        public async Task Register_Post_UserCreationFails_AddsModelErrors()
        {
            // Arrange
            var signInManager = GetSignInManager();
            var controller = new AccountController(signInManager.Object, _mockUserManager.Object, _mockLogger.Object);

            var model = new RegisterViewModel
            {
                Email = "test@example.com",
                Password = "weakpass",
                FirstName = "John",
                LastName = "Doe"
            };

            var failedResult = IdentityResult.Failed(
                new IdentityError { Description = "Password is too weak." },
                new IdentityError { Description = "Email is already taken." }
            );

            _mockUserManager.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                           .ReturnsAsync(failedResult);

            // Act
            var result = await controller.Register(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(model, viewResult.Model);
            Assert.Equal(2, controller.ModelState.ErrorCount);
            Assert.Contains("Password is too weak.", controller.ModelState[string.Empty].Errors[0].ErrorMessage);
            Assert.Contains("Email is already taken.", controller.ModelState[string.Empty].Errors[1].ErrorMessage);
        }


        [Fact]
        public async Task Logout_CallsSignOutAndRedirects()
        {
            // Arrange
            var signInManager = GetSignInManager();
            signInManager.Setup(s => s.SignOutAsync()).Returns(Task.CompletedTask);

            var controller = new AccountController(signInManager.Object, null, _mockLogger.Object);

            // Act
            var result = await controller.Logout();

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Home", redirect.ControllerName);
        }

        [Fact]
        public async Task ExternalLoginCallback_RemoteError_RedirectsToLogin()
        {
            // Arrange
            var signInManager = GetSignInManager();
            var controller = new AccountController(signInManager.Object, _mockUserManager.Object, _mockLogger.Object);

            // Act
            var result = await controller.ExternalLoginCallback("/", "SomeError");

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
        }

        [Fact]
        public async Task ExternalLoginCallback_InfoIsNull_RedirectsToLogin()
        {
            // Arrange
            var signInManager = GetSignInManager();
            signInManager.Setup(s => s.GetExternalLoginInfoAsync(null)).ReturnsAsync((ExternalLoginInfo)null);

            var controller = new AccountController(signInManager.Object, _mockUserManager.Object, _mockLogger.Object);

            // Act
            var result = await controller.ExternalLoginCallback("/");

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
        }

        [Fact]
        public async Task ExternalLoginCallback_LoginSuccess_RedirectsToReturnUrl()
        {
            // Arrange
            var info = new ExternalLoginInfo(new ClaimsPrincipal(new ClaimsIdentity([
                    new Claim(ClaimTypes.Name, "Test User"),
                    new Claim(ClaimTypes.Email, "test@example.com")
                ], "TestAuth")), "Google", "123", "Google");

            var signInManager = GetSignInManager();
            signInManager.Setup(s => s.GetExternalLoginInfoAsync(null)).ReturnsAsync(info);
            signInManager.Setup(s => s.ExternalLoginSignInAsync("Google", "123", false, true))
                .ReturnsAsync(SignInResult.Success);

            var controller = new AccountController(signInManager.Object, _mockUserManager.Object, _mockLogger.Object);

            // Act
            var result = await controller.ExternalLoginCallback("/dashboard");

            // Assert
            var redirect = Assert.IsType<LocalRedirectResult>(result);
            Assert.Equal("/dashboard", redirect.Url);
        }

        [Fact]
        public async Task ExternalLoginCallback_AccountLockedOut_RedirectsToLockout()
        {
            // Arrange
            var info = new ExternalLoginInfo(new ClaimsPrincipal(new ClaimsIdentity()), "Google", "123", "Google");

            var signInManager = GetSignInManager();
            signInManager.Setup(s => s.GetExternalLoginInfoAsync(null)).ReturnsAsync(info);
            signInManager.Setup(s => s.ExternalLoginSignInAsync("Google", "123", false, true))
                .ReturnsAsync(SignInResult.LockedOut);

            var controller = new AccountController(signInManager.Object, _mockUserManager.Object, _mockLogger.Object);

            // Act
            var result = await controller.ExternalLoginCallback("/");

            // Assert
            var redirect = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("./Lockout", redirect.PageName);
        }

        [Fact]
        public async Task ExternalLoginCallback_CreateUser_Success_RedirectsToReturnUrl()
        {
            // Arrange
            var claims = new[]
            {
                new Claim(ClaimTypes.Email, "test@example.com"),
                new Claim(ClaimTypes.GivenName, "Test"),
                new Claim(ClaimTypes.Surname, "User")
            };
            var info = new ExternalLoginInfo(new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")), "Google", "123", "Google");

            var signInManager = GetSignInManager();
            signInManager.Setup(s => s.GetExternalLoginInfoAsync(null)).ReturnsAsync(info);
            signInManager.Setup(s => s.ExternalLoginSignInAsync("Google", "123", false, true))
                .ReturnsAsync(SignInResult.Failed);
            signInManager.Setup(s => s.SignInAsync(It.IsAny<ApplicationUser>(), false, null)).Returns(Task.CompletedTask);

            _mockUserManager.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User")).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(u => u.AddLoginAsync(It.IsAny<ApplicationUser>(), info)).ReturnsAsync(IdentityResult.Success);

            var controller = new AccountController(signInManager.Object, _mockUserManager.Object, _mockLogger.Object);

            // Act
            var result = await controller.ExternalLoginCallback("/dashboard");

            // Assert
            var redirect = Assert.IsType<LocalRedirectResult>(result);
            Assert.Equal("/dashboard", redirect.Url);
        }

        [Fact]
        public async Task ExternalLoginCallback_CreateUser_Fails_ShowsErrorsAndRedirectsToLogin()
        {
            // Arrange
            var claims = new[]
            {
                new Claim(ClaimTypes.Email, "test@example.com"),
                new Claim(ClaimTypes.GivenName, "Test"),
                new Claim(ClaimTypes.Surname, "User")
            };

            var info = new ExternalLoginInfo(new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")), "Google", "123", "Google");

            var signInManager = GetSignInManager();
            signInManager.Setup(s => s.GetExternalLoginInfoAsync(null)).ReturnsAsync(info);
            signInManager.Setup(s => s.ExternalLoginSignInAsync("Google", "123", false, true))
                .ReturnsAsync(SignInResult.Failed);

            var identityErrors = new[] { new IdentityError { Description = "Test error" } };
            _mockUserManager.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Failed(identityErrors));

            var controller = new AccountController(signInManager.Object, _mockUserManager.Object, _mockLogger.Object);

            // Act
            var result = await controller.ExternalLoginCallback("/");

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
            Assert.True(controller.ModelState.ErrorCount > 0);
        }

        [Fact]
        public void AccessDenied_ReturnsView()
        {
            // Act
            var result = _controller.AccessDenied();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Profile_ReturnsView()
        {
            // Act
            var result = _controller.Profile();

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
