using Microsoft.AspNetCore.Mvc;
using SingleParentSupport2.Controllers;

namespace SingleParentSupport2.UnitTests.Controllers
{

    public class AboutControllerTests
    {
        [Fact]
        public void Index_ReturnsViewResult()
        {
            // Arrange
            var controller = new AboutController();

            // Act
            var result = controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName); // null means it uses the default view (e.g., "Index")
        }
    }
}