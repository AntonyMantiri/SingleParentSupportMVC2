using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SingleParentSupport2.Controllers;
using SingleParentSupport2.Models;

namespace SingleParentSupport2.UnitTests.Controllers
{
    public class AppointmentControllerTests
    {
        private readonly AppDbContext _dbContext;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly AppointmentController _controller;

        public AppointmentControllerTests()
        {
            // Initialize any shared resources here if needed
            _dbContext = GetInMemoryDbContext();
            _mockUserManager = GetMockUserManager();
            _controller = new AppointmentController(_dbContext, _mockUserManager.Object);
        }

        [Fact]
        public async Task Index_ReturnsAppointments_ForCurrentUser()
        {
            // Arrange
            var testUser = new ApplicationUser { Id = "test-user-id", UserName = "testuser" };
            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(testUser);

            _dbContext.Appointments.Add(new Appointment
            {
                UserId = "test-user-id",
                AppointmentDate = DateTime.Now.AddDays(1),
                Status = "Scheduled"
            });
            _dbContext.SaveChanges();

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Appointment>>(viewResult.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task Index_NoAppointments_ReturnsEmptyView()
        {
            // Arrange
            var mockUser = new ApplicationUser { Id = "user1" };
            _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                           .ReturnsAsync(mockUser);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.Model); // Since return View() is called with no model
        }

        [Fact]
        public async Task Schedule_ValidModel_SavesAppointmentAndRedirects()
        {
            // Arrange
            var testUser = new ApplicationUser { Id = "user123", FirstName = "John", LastName = "Doe" };
            _mockUserManager
                .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(testUser);
            _mockUserManager
                .Setup(um => um.Users)
                .Returns(new List<ApplicationUser> { new() { Id = "vol123", FirstName = "Jane", LastName = "Smith" } }
                .AsQueryable());

            _controller.TempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>());

            var model = new AppointmentViewModel
            {
                VolunteerId = "vol123",
                Purpose = "Counseling",
                AppointmentDate = DateTime.Today.AddDays(1),
                AppointmentTime = "10:00"
            };

            // Act
            var result = await _controller.Schedule(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Confirmation", redirectResult.ActionName);
            Assert.Single(_dbContext.Appointments);
        }

        [Fact]
        public async Task Schedule_Post_InvalidModel_ReturnsIndexWithUpcomingAppointments()
        {
            // Arrange
            _mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("user1");

            _controller.ModelState.AddModelError("Purpose", "Required");

            // Add a future appointment for user1
            _dbContext.Appointments.Add(new Appointment
            {
                Id = 1,
                UserId = "user1",
                VolunteerId = "vol1",
                AppointmentDate = DateTime.Today.AddDays(1),
                AppointmentTime = "10:00",
                Purpose = "Future Appointment",
                Status = "Scheduled",
                Volunteer = new ApplicationUser { Id = "vol1", FirstName = "Volunteer", LastName = "Test" }
            });
            _dbContext.SaveChanges();

            var model = new AppointmentViewModel
            {
                AppointmentDate = DateTime.Today,
                AppointmentTime = "10:00",
                VolunteerId = "vol1"
            };

            // Act
            var result = await _controller.Schedule(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);

            var returnedAppointments = Assert.IsAssignableFrom<List<Appointment>>(viewResult.Model);
            Assert.Single(returnedAppointments);
            Assert.Equal("Future Appointment", returnedAppointments[0].Purpose);
        }

        [Fact]
        public async Task Reschedule_Post_InvalidModel_ReturnsViewWithModel()
        {
            // Arrange
            _controller.ModelState.AddModelError("Purpose", "Required");

            var model = new AppointmentViewModel
            {
                AppointmentId = 1,
                AppointmentDate = DateTime.Today,
                AppointmentTime = "10:00",
                Purpose = "",
                VolunteerId = "vol1"
            };

            // Act
            var result = await _controller.Reschedule(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
        }

        [Fact]
        public async Task Reschedule_Post_AppointmentNotFound_ReturnsNotFound()
        {
            // Arrange
            var model = new AppointmentViewModel
            {
                AppointmentId = 999, // Non-existent ID
                AppointmentDate = DateTime.Today,
                AppointmentTime = "10:00",
                Purpose = "Update",
                VolunteerId = "vol1"
            };

            // Act
            var result = await _controller.Reschedule(model);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Reschedule_Post_ValidModel_UpdatesAppointmentAndRedirects()
        {
            // Arrange
            var appointment = new Appointment
            {
                Id = 1,
                AppointmentDate = DateTime.Today,
                AppointmentTime = "09:00",
                Purpose = "Old Purpose",
                VolunteerId = "vol1",
                Status = "Scheduled"
            };

            _dbContext.Appointments.Add(appointment);
            _dbContext.SaveChanges();

            var model = new AppointmentViewModel
            {
                AppointmentId = 1,
                AppointmentDate = DateTime.Today.AddDays(1),
                AppointmentTime = "11:00",
                Purpose = "New Purpose",
                VolunteerId = "vol2"
            };

            // Act
            var result = await _controller.Reschedule(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            var updated = _dbContext.Appointments.First(a => a.Id == 1);
            Assert.Equal("11:00", updated.AppointmentTime);
            Assert.Equal("New Purpose", updated.Purpose);
            Assert.Equal("Rescheduled", updated.Status);
            Assert.Equal("vol2", updated.VolunteerId);
        }

        [Fact]
        public async Task Cancel_ExistingAppointment_ChangesStatusToCancelled()
        {
            // Arrange
            var appointment = new Appointment
            {
                Id = 1,
                Status = "Scheduled"
            };

            _dbContext.Appointments.Add(appointment);
            _dbContext.SaveChanges();

            // Act
            var result = await _controller.Cancel(1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Cancelled", _dbContext.Appointments.Find(1).Status);
        }

        [Fact]
        public async Task Cancel_NonExistingAppointment_ReturnsNotFound()
        {
            // Act
            var result = await _controller.Cancel(999); // non-existent ID

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void GetAvailableTime_ReturnsCorrectAvailability()
        {
            // Arrange
            var volunteerId = "vol123";
            var testDate = DateTime.Today;

            // Book two slots: 09:00 and 11:00
            _dbContext.Appointments.AddRange(
                new Appointment
                {
                    VolunteerId = volunteerId,
                    AppointmentDate = testDate,
                    AppointmentTime = "09:00",
                    Status = "Scheduled"
                },
                new Appointment
                {
                    VolunteerId = volunteerId,
                    AppointmentDate = testDate,
                    AppointmentTime = "11:00",
                    Status = "Scheduled"
                });

            _dbContext.SaveChanges();

            // Act
            var result = _controller.GetAvailableTime(volunteerId, testDate) as JsonResult;
            var availableTimes = result.Value as List<AvailableTime>;

            // Assert
            Assert.NotNull(availableTimes);
            Assert.Equal(13, availableTimes.Count); // 9 AM to 9 PM
            Assert.False(availableTimes.First(t => t.Time == "09:00").IsAvailable);
            Assert.False(availableTimes.First(t => t.Time == "11:00").IsAvailable);
            Assert.All(availableTimes.Where(t => t.Time != "09:00" && t.Time != "11:00"), t => Assert.True(t.IsAvailable));
        }

        [Fact]
        public async Task GetAllAppointments_ReturnsAppointmentsJson()
        {
            // Arrange
            var user = new ApplicationUser { Id = "user1", FirstName = "Alice", LastName = "Smith" };
            var volunteer = new ApplicationUser { Id = "vol1", FirstName = "Bob", LastName = "Jones" };

            _dbContext.Users.AddRange(user, volunteer);
            _dbContext.Appointments.Add(new Appointment
            {
                UserId = user.Id,
                VolunteerId = volunteer.Id,
                User = user,
                Volunteer = volunteer,
                AppointmentDate = DateTime.Now.AddDays(1),
                AppointmentTime = "10:00",
                Purpose = "Parenting Help",
                Status = "Scheduled"
            });

            _dbContext.SaveChanges();

            // Act
            var result = await _controller.GetAllAppointments() as JsonResult;

            // Assert
            Assert.NotNull(result);

            // Serialize and deserialize to List<Dictionary<string, object>>
            var jsonString = JsonSerializer.Serialize(result.Value);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var appointments = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonString, options);

            Assert.Single(appointments);

            var appointment = appointments[0];

            Assert.Equal("Alice Smith", appointment["User"].ToString());
            Assert.Equal("Bob Jones", appointment["Volunteer"].ToString());
            Assert.Equal("10:00", appointment["Time"].ToString());
            Assert.Equal("Parenting Help", appointment["Purpose"].ToString());
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
    }
}