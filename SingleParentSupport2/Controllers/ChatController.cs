using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SingleParentSupport2.Models;

namespace SingleParentSupport2.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        public async Task<IActionResult> Index(string receiverId)
        {
            var userId = _userManager.GetUserId(User);

            // Get all chat partners
            var chatPartners = await _context.ChatLogs
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .Select(m => m.SenderId == userId ? m.Receiver : m.Sender)
                .Distinct()
                .ToListAsync();

            // Fix: Retrieve the receiverId from the first chat partner's Id directly
            receiverId = chatPartners.FirstOrDefault()?.Id;

            var chatRooms = chatPartners.Select(p => new ChatRoomViewModel
            {
                Id = p.Id,
                Name = p.UserName,
                AvatarUrl = "https://via.placeholder.com/50",
                LastMessage = _context.ChatLogs
                    .Where(m => (m.SenderId == userId && m.ReceiverId == p.Id) || (m.SenderId == p.Id && m.ReceiverId == userId))
                    .OrderByDescending(m => m.Timestamp)
                    .Select(m => m.Content)
                    .FirstOrDefault(),
                LastMessageTime = _context.ChatLogs
                    .Where(m => (m.SenderId == userId && m.ReceiverId == p.Id) || (m.SenderId == p.Id && m.ReceiverId == userId))
                    .OrderByDescending(m => m.Timestamp)
                    .Select(m => m.Timestamp.ToShortTimeString())
                    .FirstOrDefault(),
                UnreadCount = _context.ChatLogs
                    .Count(m => m.SenderId == p.Id && m.ReceiverId == userId && !m.IsRead)
            }).ToList();

            // Get messages for selected room
            var messages = await _context.ChatLogs
                .Where(m => (m.SenderId == userId && m.ReceiverId == receiverId) || (m.SenderId == receiverId && m.ReceiverId == userId))
                .OrderBy(m => m.Timestamp)
                .Select(m => new ChatMessageViewModel
                {
                    MessageId = m.Id,
                    SenderId = m.SenderId,
                    ReceiverId = m.ReceiverId,
                    SenderName = m.Sender.UserName,
                    AvatarUrl = "https://via.placeholder.com/40",
                    Content = m.Content,
                    Timestamp = m.Timestamp,
                    IsOutgoing = m.SenderId == userId
                })
                .ToListAsync();

            var viewModel = new ChatPageViewModel
            {
                ChatRooms = chatRooms,
                Messages = messages,
                ActiveRoomId = receiverId
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatLog model)
        {
            if (ModelState.IsValid)
            {
                var senderId = _userManager.GetUserId(User);
                var message = new ChatLog
                {
                    SenderId = senderId,
                    ReceiverId = model.ReceiverId,
                    Content = model.Content,
                    Timestamp = DateTime.UtcNow,
                    IsRead = false
                };

                _context.ChatLogs.Add(message);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }

            return Json(new { success = false });
        }
    }
}
