using System.ComponentModel.DataAnnotations;

namespace SingleParentSupport2.Models
{
    public class SupportRequestResponseViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string RequestType { get; set; }

        public string Description { get; set; }

        public DateTime RequestDate { get; set; }

        [Required(ErrorMessage = "Please select a status")]
        public string Status { get; set; }

        [Required(ErrorMessage = "Response is required")]
        [MinLength(10, ErrorMessage = "Response must be at least 10 characters")]
        public string Response { get; set; }
    }
}
