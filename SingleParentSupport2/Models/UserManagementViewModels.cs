using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SingleParentSupport2.Models
{
    public class UserManagementViewModel
    {

        public string Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public List<string> Roles { get; set; }
        public DateTime JoinDate { get; set; }
        public bool IsVolunteer { get; set; }
        public string VolunteerRole { get; set; }
        public string VolunteerBio { get; set; }

    }

    public class CreateUserViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Display(Name = "Is Volunteer")]
        public bool IsVolunteer { get; set; }

        [Display(Name = "Volunteer Role")]
        public string VolunteerRole { get; set; }

        [Display(Name = "Volunteer Bio")]
        public string VolunteerBio { get; set; }

        [Display(Name = "Roles")]
        public List<string> SelectedRoles { get; set; }

        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> AllRoles { get; set; }
    }
}
