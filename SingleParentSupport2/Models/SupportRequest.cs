using System;

namespace SingleParentSupport2.Models
{
    public class SupportRequest
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string RequestType { get; set; }
        public string Description { get; set; }
        public DateTime RequestDate { get; set; }
        public string Status { get; set; }
        public string Response { get; set; }
        public DateTime? ResponseDate { get; set; }
        public string ResponderId { get; set; }

        // Navigation property
        public ApplicationUser User { get; set; }
    }
}
