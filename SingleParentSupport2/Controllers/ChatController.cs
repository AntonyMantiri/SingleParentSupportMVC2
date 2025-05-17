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
            .Select(m => new
            {
                Partner = m.SenderId == userId ? m.Receiver : m.Sender,
                PartnerId = m.SenderId == userId ? m.ReceiverId : m.SenderId
            })
            .GroupBy(x => x.PartnerId)
            .Select(g => g.First().Partner)
            .ToListAsync();


            // Fix: Retrieve the receiverId from the first chat partner's Id directly
            if (string.IsNullOrEmpty(receiverId))
            {
                receiverId = chatPartners.FirstOrDefault()?.Id;
            }

            var chatRooms = chatPartners.Select(p => new ChatRoomViewModel
            {
                Id = p.Id,
                Name = p.FirstName + " " + p.LastName,
                AvatarUrl = GetAvatar(p.Id),
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
                    AvatarUrl = GetAvatar(m.SenderId),
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage([FromBody] ChatLog model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var senderId = _userManager.GetUserId(User);
                    if (string.IsNullOrEmpty(senderId))
                        return Json(new { success = false, message = "User not authenticated." });

                    var message = new ChatLog
                    {
                        SenderId = senderId,
                        ReceiverId = model.ReceiverId,
                        Content = model.Content,
                        Timestamp = DateTime.UtcNow,
                        IsRead = false
                    };

                    var avatarUrl = GetAvatar(senderId);

                    _context.ChatLogs.Add(message);
                    await _context.SaveChangesAsync();

                    return Json(new { success = true, avatarUrl });
                }
                return Json(new { success = false, message = "Model invalid." });
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = "Server error: " + innerMessage });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages(string receiverId)
        {
            var userId = _userManager.GetUserId(User);

            // Mark all unread messages as read
            var unreadMessages = await _context.ChatLogs
                .Where(m => ((m.SenderId == userId && m.ReceiverId == receiverId) || (m.SenderId == receiverId && m.ReceiverId == userId)) && !m.IsRead)
                .ToListAsync();

            if (unreadMessages.Count > 0)
            {
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }

                await _context.SaveChangesAsync();
            }

            var messages = await _context.ChatLogs
                .Where(m => (m.SenderId == userId && m.ReceiverId == receiverId) || (m.SenderId == receiverId && m.ReceiverId == userId))
                .OrderBy(m => m.Timestamp)
                .Select(m => new
                {
                    messageId = m.Id,
                    senderId = m.SenderId,
                    receiverId = m.ReceiverId,
                    senderName = m.Sender.UserName,
                    avatarUrl = GetAvatar(m.SenderId),
                    content = m.Content,
                    timestamp = m.Timestamp.ToString("g"),
                    isOutgoing = m.SenderId == userId
                })
                .ToListAsync();

            return Json(messages);
        }

        private static string GetAvatar(string id)
        {
            return id switch
            {
                "9c5e771c-0c84-4ff9-b6cc-b821b245eaa1" => "/images/sarahjohnson.jpeg",
                "a6cef287-368e-4590-97d7-4ecd8ded38c0" => "/images/johndoe.jpeg",
                "227f1609-a235-4d1c-884a-aefb1fa542d0" => "/images/michaelbrown.jpeg",
                "5e78e6f8-ad86-4a7e-86ec-883669ececb9" => "/images/brucewayne.jpeg",
                _ => ""
            };
        }
    }
}
