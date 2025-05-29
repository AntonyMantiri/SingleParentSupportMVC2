using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SingleParentSupport2.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        // Original one-to-one chat method
        public async Task SendMessage(string message, string receiverId)
        {
            var sender = Context.User;
            var senderId = Context.UserIdentifier;
            var senderName = sender.Identity.Name;
            var senderRole = sender.IsInRole("Volunteer") ? "Volunteer" : "User";

            // Send to specific user if receiverId is provided
            if (!string.IsNullOrEmpty(receiverId))
            {
                // Send to the receiver
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

        // GROUP CHAT METHODS

        // Create a new group
        public async Task CreateGroup(string groupName, string groupDescription)
        {
            var creator = Context.User;
            var creatorId = Context.UserIdentifier;
            var creatorName = creator.Identity.Name;

            // Add creator to the group
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            // Notify everyone that a new group was created (for discovery)
            await Clients.All.SendAsync("GroupCreated", groupName, groupDescription, creatorId, creatorName);

            // Notify the creator they've joined their own group
            await Clients.Caller.SendAsync("JoinedGroup", groupName);
        }

        // Join an existing group
        public async Task JoinGroup(string groupName)
        {
            var user = Context.User;
            var userId = Context.UserIdentifier;
            var userName = user.Identity.Name;
            var userRole = user.IsInRole("Volunteer") ? "Volunteer" : "User";

            // Add user to the group
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            // Notify the group that someone joined
            await Clients.Group(groupName).SendAsync("UserJoinedGroup", userId, userName, userRole, groupName);

            // Notify the user they've joined successfully
            await Clients.Caller.SendAsync("JoinedGroup", groupName);
        }

        // Leave a group
        public async Task LeaveGroup(string groupName)
        {
            var user = Context.User;
            var userId = Context.UserIdentifier;
            var userName = user.Identity.Name;

            // Remove user from the group
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

            // Notify the group that someone left
            await Clients.Group(groupName).SendAsync("UserLeftGroup", userId, userName, groupName);
        }

        // Send a message to a group
        public async Task SendGroupMessage(string message, string groupName)
        {
            var sender = Context.User;
            var senderId = Context.UserIdentifier;
            var senderName = sender.Identity.Name;
            var senderRole = sender.IsInRole("Volunteer") ? "Volunteer" : "User";
            var timestamp = DateTime.Now.ToString("g");

            // Send to everyone in the group
            await Clients.Group(groupName).SendAsync("ReceiveGroupMessage",
                senderId, senderName, message, senderRole, groupName, timestamp);
        }

        // Original connection methods
        public override async Task OnConnectedAsync()
        {
            // You could add users to groups here
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // Clean up when users disconnect
            await base.OnDisconnectedAsync(exception);
        }
    }
}
