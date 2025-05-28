using Microsoft.AspNetCore.Mvc;
using SingleParentSupport2.Controllers;
using SingleParentSupport2.Models;

namespace SingleParentSupport2.UnitTests.Controllers
{
    public class ContactControllerTests
    {
        [Fact]
        public void Index_ReturnsViewResult()
        {
            // Arrange
            var controller = new ContactController();

            // Act
            var result = controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName); // default view
        }

        [Fact]
        public void Submit_ValidModel_RedirectsToThankYou()
        {
            // Arrange
            var controller = new ContactController();
            controller.ModelState.Clear(); // ensure model is valid
            var model = new ContactViewModel(); // use valid dummy model

            // Act
            var result = controller.Submit(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ThankYou", redirectResult.ActionName);
        }

        [Fact]
        public void Submit_InvalidModel_ReturnsIndexViewWithModel()
        {
            // Arrange
            var controller = new ContactController();
            controller.ModelState.AddModelError("Email", "Required");
            var model = new ContactViewModel { Name = "Test" };

            // Act
            var result = controller.Submit(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            Assert.Equal(model, viewResult.Model);
        }

        [Fact]
        public void ThankYou_ReturnsViewResult()
        {
            // Arrange
            var controller = new ContactController();

            // Act
            var result = controller.ThankYou();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName); // default view
        }
    }
}