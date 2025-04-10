using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;
using SingleParentSupport2.Models;

namespace SingleParentSupport2.Models
{
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }

        public string ReturnUrl { get; set; }
    }

    public class AccessDeniedViewModel
    {
        public string ReturnUrl { get; set; }
    }

    public class ProfileViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ProfilePicture { get; set; }
        public DateTime JoinDate { get; set; }
        public bool IsVolunteer { get; set; }
        public string VolunteerRole { get; set; }
        public string VolunteerBio { get; set; }
        public List<string> Roles { get; set; }
    }

    // New This class is used for the registration process
    public class RegisterViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 8)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The passwords do not match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        // Optional: Add fields like phone number if needed
    }
}
