using Microsoft.AspNetCore.Mvc;
using System;

namespace SingleParentSupport2.Models
{
    public class SupportRequestViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string RequestType { get; set; }
        public string Description { get; set; }
        public DateTime RequestDate { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Pending";
    }

    public class AppointmentViewModel
    {
        public int AppointmentId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string AppointmentTime { get; set; }
        public string VolunteerId { get; set; }
        public string Purpose { get; set; }
        public string Status { get; set; } = "Scheduled";
    }

    //public class ChatMessageViewModel
    //{
    //    public int MessageId { get; set; }
    //    public int SenderId { get; set; }
    //    public int ReceiverId { get; set; }
    //    public string Content { get; set; }
    //    public DateTime Timestamp { get; set; } = DateTime.Now;
    //    public bool IsRead { get; set; } = false;
    //}


    public class ChatRoomViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string AvatarUrl { get; set; }
        public string LastMessage { get; set; }
        public string LastMessageTime { get; set; }
        public int UnreadCount { get; set; }
    }

    public class ChatMessageViewModel
    {
        public int MessageId { get; set; }
        public string SenderId { get; set; }
        public string ReceiverId { get; set; }
        public string SenderName { get; set; }
        public string AvatarUrl { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsOutgoing { get; set; }
    }

    public class ChatPageViewModel
    {
        public List<ChatRoomViewModel> ChatRooms { get; set; }
        public List<ChatMessageViewModel> Messages { get; set; }
        public string ActiveRoomId { get; set; }
    }


    public class ContactViewModel
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
    }
}
