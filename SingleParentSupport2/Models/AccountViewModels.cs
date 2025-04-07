using System.ComponentModel.DataAnnotations;
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
}
