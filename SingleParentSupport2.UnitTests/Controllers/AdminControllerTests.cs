using Microsoft.AspNetCore.Mvc;
using SingleParentSupport2.Controllers;

namespace SingleParentSupport2.UnitTests.Controllers
{
    public class AdminControllerTests
    {
        [Fact]
        public void Index_ReturnsViewResult()
        {
            // Arrange
            var controller = new AdminController();

            // Act
            var result = controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName); // uses default view name
        }
    }
}