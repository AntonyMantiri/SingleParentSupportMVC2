using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace SingleParentSupport2.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        public async Task SendMessage(string message, string receiverId)
        {
            var sender = Context.User;
            var senderId = Context.UserIdentifier;
            var senderName = sender.Identity.Name;
            var senderRole = sender.IsInRole("Volunteer") ? "Volunteer" : "User";

            // Send to specific user if receiverId is provided
            if (!string.IsNullOrEmpty(receiverId))
            {
                // Send to the receiver - ensure parameter order matches chat.js expectations
                await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, senderName, message, senderRole);

                // Also send back to sender for their chat window
                await Clients.Caller.SendAsync("ReceiveMessage", senderId, senderName, message, senderRole);
            }
            else
            {
                // If no specific receiver, send to all users (group chat)
                await Clients.All.SendAsync("ReceiveMessage", senderId, senderName, message, senderRole);
            }
        }

        public async Task JoinChat(string userId)
        {
            // This method can be used to join specific chat rooms or groups
            await Clients.Caller.SendAsync("JoinedChat", userId);
        }

        public override async Task OnConnectedAsync()
        {
            // Log connection or add users to groups here if needed
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // Clean up when users disconnect
            await base.OnDisconnectedAsync(exception);
        }
    }
}
