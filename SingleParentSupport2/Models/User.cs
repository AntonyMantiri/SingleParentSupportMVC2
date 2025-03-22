using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SingleParentSupport2.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string Role { get; set; }

        public DateTime RegistrationDate { get; set; }

        // Navigation properties
        public ICollection<SupportRequest> SupportRequests { get; set; }
        public ICollection<Appointment> Appointments { get; set; }
        public ICollection<ChatLog> ChatLogs { get; set; }
    }
}