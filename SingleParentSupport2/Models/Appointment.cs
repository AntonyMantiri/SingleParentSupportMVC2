using System;

namespace SingleParentSupport2.Models
{
    public class Appointment
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string VolunteerId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string AppointmentTime { get; set; }
        public string Purpose { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }

        // Navigation properties
        public ApplicationUser User { get; set; }
        public ApplicationUser Volunteer { get; set; }
    }
}
