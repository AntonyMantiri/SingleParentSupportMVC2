using System.ComponentModel.DataAnnotations;

namespace SingleParentSupport2.Models
{
    public class UserEditViewModel
    {
        public string Id { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

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

        public List<string> SelectedRoles { get; set; } = new List<string>();
        public List<string> CurrentRoles { get; set; } = new List<string>();
        public List<string> AllRoles { get; set; } = new List<string>();
    }

    public class ChangePasswordViewModel
    {
        public string UserId { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }
}
