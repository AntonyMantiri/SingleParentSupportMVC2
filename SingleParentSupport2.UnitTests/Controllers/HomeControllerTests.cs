using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SingleParentSupport2.Controllers;
using SingleParentSupport2.Models;

namespace SingleParentSupport2.UnitTests.Controllers
{
    public class HomeControllerTests
    {
        private readonly Mock<ILogger<HomeController>> _mockLogger;

        public HomeControllerTests()
        {
            _mockLogger = new Mock<ILogger<HomeController>>();
        }

        [Fact]
        public void Index_ReturnsViewResult()
        {
            var controller = new HomeController(_mockLogger.Object);

            var result = controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName); // default view
        }

        [Fact]
        public void Privacy_ReturnsViewResult()
        {
            var controller = new HomeController(_mockLogger.Object);

            var result = controller.Privacy();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName); // default view
        }

        [Fact]
        public void Error_ReturnsViewResultWithErrorViewModel()
        {
            var controller = new HomeController(_mockLogger.Object);

            // Simulate HttpContext.TraceIdentifier
            var httpContext = new DefaultHttpContext();
            httpContext.TraceIdentifier = "trace-123";
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            var result = controller.Error();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ErrorViewModel>(viewResult.Model);
            Assert.Equal("trace-123", model.RequestId);
        }
    }
}