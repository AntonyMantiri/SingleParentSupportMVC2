using System;
using System.ComponentModel.DataAnnotations;

namespace SingleParentSupport2.Models
{
    public class SupportRequest
    {
        [Key]
        public int RequestId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public string RequestType { get; set; }

        [Required]
        public string RequestText { get; set; }

        public DateTime RequestDate { get; set; }

        [Required]
        public string Status { get; set; }

        // Navigation property
        public User User { get; set; }
    }
}