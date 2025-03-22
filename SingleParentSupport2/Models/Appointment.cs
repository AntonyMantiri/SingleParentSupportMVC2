using System;
using System.ComponentModel.DataAnnotations;

namespace SingleParentSupport2.Models
{
    public class Appointment
    {
        [Key]
        public int AppointmentId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int VolunteerId { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; }

        [Required]
        public string Purpose { get; set; }

        [Required]
        public string Status { get; set; }

        // Navigation properties
        public User User { get; set; }
        public User Volunteer { get; set; }
    }
}