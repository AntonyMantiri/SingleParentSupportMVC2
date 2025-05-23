using Microsoft.AspNetCore.Mvc;
using SingleParentSupport2.Controllers;

namespace SingleParentSupport2.UnitTests.Controllers
{
    public class ServicesControllerTests
    {
        [Fact]
        public void Index_ReturnsViewResult()
        {
            // Arrange
            var controller = new ServicesController();

            // Act
            var result = controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName); // Ensures it returns default view
        }
    }
}