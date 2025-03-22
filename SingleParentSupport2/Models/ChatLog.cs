using System;
using System.ComponentModel.DataAnnotations;

namespace SingleParentSupport2.Models
{
    public class ChatLog
    {
        [Key]
        public int ChatLogId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public string Message { get; set; }

        public DateTime Timestamp { get; set; }

        // Navigation property
        public User User { get; set; }
    }
}