"use strict";

// Get the current user ID from a data attribute
const currentUserId = document.querySelector('meta[name="user-id"]')?.content;
let selectedUserId = null;
let connection = null;
let activeGroup = null;
let groups = [];
let userPhotoCache = {}; // Cache for user photos

// Initialize the SignalR connection
function initializeSignalRConnection() {
    // Create the connection
    connection = new signalR.HubConnectionBuilder()
        .withUrl("/chatHub")
        .withAutomaticReconnect()
        .build();

    // Handle receiving messages (one-to-one chat)
    connection.on("ReceiveMessage", (senderId, senderName, message, senderRole) => {
        // Only display messages from the selected user or if no user is selected
        if (!selectedUserId || senderId === selectedUserId || senderId === currentUserId) {
            displayMessage(senderId, senderName, message, senderRole);
        }
    });

    // Start the connection
    startConnection();

    // Set up UI event handlers
    setupEventHandlers();

    // Initialize photo cache from user list
    initializePhotoCache();
}

// Initialize photo cache from user list
function initializePhotoCache() {
    document.querySelectorAll('.chat-user').forEach(userItem => {
        const userId = userItem.getAttribute('data-user-id');
        const userName = userItem.getAttribute('data-user-name');
        const photoElement = userItem.querySelector('.chat-avatar img');

        if (photoElement) {
            const photoUrl = photoElement.getAttribute('src');
            if (photoUrl) {
                userPhotoCache[userId] = {
                    name: userName,
                    photoUrl: photoUrl
                };
            }
        }
    });

    console.log('Photo cache initialized:', userPhotoCache);
}

// Start the SignalR connection
function startConnection() {
    connection.start()
        .then(() => {
            console.log("SignalR Connected");
            document.getElementById("chatStatus").textContent = "Connected";
            document.getElementById("chatStatus").classList.remove("bg-danger");
            document.getElementById("chatStatus").classList.add("bg-success");
        })
        .catch(err => {
            console.error(err);
            document.getElementById("chatStatus").textContent = "Disconnected";
            document.getElementById("chatStatus").classList.remove("bg-success");
            document.getElementById("chatStatus").classList.add("bg-danger");

            // Try to reconnect after 5 seconds
            setTimeout(startConnection, 5000);
        });
}

// Set up event handlers for the UI
function setupEventHandlers() {
    // Handle form submission
    const messageForm = document.getElementById("messageForm");
    messageForm.addEventListener("submit", function (event) {
        event.preventDefault();
        sendMessage();
    });

    // Handle clicking on a user in the list
    const usersList = document.getElementById("usersList");
    usersList.addEventListener("click", function (event) {
        const userItem = event.target.closest(".chat-user");
        if (userItem) {
            selectUser(userItem);
        }
    });
}

// Send a message
function sendMessage() {
    const messageInput = document.getElementById("messageInput");
    const message = messageInput.value.trim();

    if (message) {
        if (activeGroup) {
            // Send as group message
            connection.invoke("SendGroupMessage", message, activeGroup)
                .catch(err => console.error(err));
        } else {
            // Send as direct message
            connection.invoke("SendMessage", message, selectedUserId)
                .catch(err => console.error(err));
        }

        messageInput.value = "";
    }

    // Focus back on the input
    messageInput.focus();
}

// Select a user to chat with
function selectUser(userItem) {
    // Reset group state
    activeGroup = null;

    // Remove active class from all users and groups
    document.querySelectorAll(".chat-user, .chat-group").forEach(item => {
        item.classList.remove("active");
    });

    // Add active class to selected user
    userItem.classList.add("active");

    // Get user ID and update the hidden input
    selectedUserId = userItem.getAttribute("data-user-id");
    document.getElementById("receiverId").value = selectedUserId;

    // Update chat title
    const userName = userItem.querySelector(".fw-bold").textContent;
    document.getElementById("chatTitle").textContent = `Chat with ${userName}`;

    // Clear the messages list
    document.getElementById("messagesList").innerHTML = "";
}

// Get user photo URL from cache or default
function getUserPhotoUrl(userId, userName) {
    if (userPhotoCache[userId] && userPhotoCache[userId].photoUrl) {
        return userPhotoCache[userId].photoUrl;
    }

    // Check if this is one of our known volunteers by name
    const normalizedName = userName.toLowerCase().replace(/\s+/g, '');
    if (normalizedName === 'johndoe') {
        return '/images/johndoe.jpeg';
    } else if (normalizedName === 'michaelbrown') {
        return '/images/michaelbrown.jpeg';
    } else if (normalizedName === 'sarahjohnson') {
        return '/images/sarahjohnson.jpeg';
    }

    // Default avatar
    return null;
}

// Display a message in the chat
function displayMessage(senderId, senderName, message, senderRole) {
    const messagesList = document.getElementById("messagesList");

    // Create message element
    const messageDiv = document.createElement("div");
    messageDiv.className = senderId === currentUserId ? "message message-sent" : "message message-received";

    // Get photo URL for this user
    const photoUrl = getUserPhotoUrl(senderId, senderName);

    // Create message avatar if we have a photo
    if (photoUrl) {
        const avatarDiv = document.createElement("div");
        avatarDiv.className = "message-avatar";

        const avatarImg = document.createElement("img");
        avatarImg.src = photoUrl;
        avatarImg.alt = senderName;

        avatarDiv.appendChild(avatarImg);
        messageDiv.appendChild(avatarDiv);
    }

    // Create message info element
    const messageInfo = document.createElement("div");
    messageInfo.className = "message-info";
    messageInfo.textContent = senderName;

    // Create role badge
    const roleBadge = document.createElement("span");
    roleBadge.className = `message-role role-${senderRole}`;
    roleBadge.textContent = senderRole;
    messageInfo.appendChild(roleBadge);

    // Create message content
    const messageContent = document.createElement("div");
    messageContent.textContent = message;

    // Assemble message
    messageDiv.appendChild(messageInfo);
    messageDiv.appendChild(messageContent);

    // Add to messages list
    messagesList.appendChild(messageDiv);

    // Scroll to bottom
    messagesList.scrollTop = messagesList.scrollHeight;
}

// GROUP CHAT FUNCTIONS

// Initialize group chat functionality
function initializeGroupChat() {
    // Handle group-related events
    connection.on("GroupCreated", (groupName, groupDescription, creatorId, creatorName) => {
        console.log("Group created:", groupName);
        addGroupToList(groupName, groupDescription, creatorId);
    });

    connection.on("JoinedGroup", (groupName) => {
        console.log("Joined group:", groupName);
        // Update UI to show we've joined
        activeGroup = groupName;
        selectedUserId = null; // Reset selected user

        // Update chat title
        document.getElementById("chatTitle").textContent = `Group: ${groupName}`;

        // Clear messages
        document.getElementById("messagesList").innerHTML = "";

        // Update active state in groups list
        document.querySelectorAll(".chat-group").forEach(g => {
            g.classList.toggle("active", g.getAttribute("data-group-name") === groupName);
        });

        // Remove active class from all users
        document.querySelectorAll(".chat-user").forEach(u => {
            u.classList.remove("active");
        });
    });

    connection.on("UserJoinedGroup", (userId, userName, userRole, groupName) => {
        console.log("User joined group:", userName);
        if (activeGroup === groupName) {
            // Add system message
            const messagesList = document.getElementById("messagesList");
            const notification = document.createElement("div");
            notification.className = "text-center my-2";
            notification.innerHTML = `<small class="text-muted">${userName} (${userRole}) joined the group</small>`;
            messagesList.appendChild(notification);
            messagesList.scrollTop = messagesList.scrollHeight;
        }
    });

    connection.on("UserLeftGroup", (userId, userName, groupName) => {
        console.log("User left group:", userName);
        if (activeGroup === groupName) {
            // Add system message
            const messagesList = document.getElementById("messagesList");
            const notification = document.createElement("div");
            notification.className = "text-center my-2";
            notification.innerHTML = `<small class="text-muted">${userName} left the group</small>`;
            messagesList.appendChild(notification);
            messagesList.scrollTop = messagesList.scrollHeight;
        }
    });

    connection.on("ReceiveGroupMessage", (senderId, senderName, message, senderRole, groupName, timestamp) => {
        console.log("Group message received:", message);
        if (activeGroup === groupName) {
            displayGroupMessage(senderId, senderName, message, senderRole, timestamp);
        }
    });

    // Set up UI event handlers for group chat
    const createGroupBtn = document.getElementById("createGroupBtn");
    if (createGroupBtn) {
        createGroupBtn.addEventListener("click", function () {
            console.log("Create group button clicked");
            createGroup();
        });
    }

    const confirmCreateGroupBtn = document.getElementById("confirmCreateGroupBtn");
    if (confirmCreateGroupBtn) {
        confirmCreateGroupBtn.addEventListener("click", function () {
            console.log("Confirm create group button clicked");
            createGroupFromModal();
        });
    }
}

// Add a group to the UI list
function addGroupToList(groupName, groupDescription, creatorId) {
    console.log("Adding group to list:", groupName);
    const groupsList = document.getElementById("groupsList");
    if (!groupsList) {
        console.error("Groups list element not found");
        return;
    }

    // Remove placeholder if present
    const placeholder = groupsList.querySelector(".text-muted");
    if (placeholder) {
        placeholder.remove();
    }

    // Create group item
    const groupItem = document.createElement("li");
    groupItem.className = "list-group-item chat-group";
    groupItem.setAttribute("data-group-name", groupName);
    groupItem.innerHTML = `
        <div class="d-flex justify-content-between align-items-center">
            <div>
                <div class="fw-bold">${groupName}</div>
                <small class="text-muted">${groupDescription || "No description"}</small>
            </div>
            <button class="btn btn-sm btn-outline-primary join-group-btn">Join</button>
        </div>
    `;

    // Add event listener to join button
    const joinBtn = groupItem.querySelector(".join-group-btn");
    joinBtn.addEventListener("click", function (e) {
        e.stopPropagation(); // Prevent the li click event
        console.log("Join group button clicked for:", groupName);
        connection.invoke("JoinGroup", groupName)
            .catch(err => console.error("Error joining group:", err));
    });

    // Add click event to the whole group item
    groupItem.addEventListener("click", function () {
        if (this.classList.contains("active")) return;

        console.log("Group item clicked:", groupName);
        connection.invoke("JoinGroup", groupName)
            .catch(err => console.error("Error joining group:", err));
    });

    // Add to list
    groupsList.appendChild(groupItem);

    // Add to groups array
    groups.push({
        name: groupName,
        description: groupDescription,
        creatorId: creatorId
    });
}

// Create a new group (simple version)
function createGroup() {
    const groupNameInput = document.getElementById("groupNameInput");
    if (!groupNameInput) {
        console.error("Group name input not found");
        return;
    }

    const groupName = groupNameInput.value.trim();

    if (groupName) {
        console.log("Creating group:", groupName);
        connection.invoke("CreateGroup", groupName, "")
            .then(() => {
                // Clear input
                groupNameInput.value = "";
            })
            .catch(err => console.error("Error creating group:", err));
    }
}

// Create a new group from modal
function createGroupFromModal() {
    const groupNameInput = document.getElementById("groupNameInput");
    const groupDescriptionInput = document.getElementById("groupDescriptionInput");

    if (!groupNameInput || !groupDescriptionInput) {
        console.error("Group inputs not found");
        return;
    }

    const groupName = groupNameInput.value.trim();
    const groupDescription = groupDescriptionInput.value.trim();

    if (groupName) {
        console.log("Creating group from modal:", groupName);
        connection.invoke("CreateGroup", groupName, groupDescription)
            .then(() => {
                // Close modal if it exists
                const modal = document.getElementById("createGroupModal");
                if (modal && typeof bootstrap !== 'undefined') {
                    const bsModal = bootstrap.Modal.getInstance(modal);
                    if (bsModal) bsModal.hide();
                }

                // Clear inputs
                groupNameInput.value = "";
                groupDescriptionInput.value = "";
            })
            .catch(err => console.error("Error creating group from modal:", err));
    }
}

// Display a group message
function displayGroupMessage(senderId, senderName, message, senderRole, timestamp) {
    const messagesList = document.getElementById("messagesList");

    // Create message element
    const messageDiv = document.createElement("div");
    messageDiv.className = senderId === currentUserId ? "message message-sent" : "message message-received";

    // Get photo URL for this user
    const photoUrl = getUserPhotoUrl(senderId, senderName);

    // Create message avatar if we have a photo
    if (photoUrl) {
        const avatarDiv = document.createElement("div");
        avatarDiv.className = "message-avatar";

        const avatarImg = document.createElement("img");
        avatarImg.src = photoUrl;
        avatarImg.alt = senderName;

        avatarDiv.appendChild(avatarImg);
        messageDiv.appendChild(avatarDiv);
    }

    // Create message info element
    const messageInfo = document.createElement("div");
    messageInfo.className = "message-info";
    messageInfo.textContent = senderName;

    // Create role badge
    const roleBadge = document.createElement("span");
    roleBadge.className = `message-role role-${senderRole}`;
    roleBadge.textContent = senderRole;
    messageInfo.appendChild(roleBadge);

    // Create message content
    const messageContent = document.createElement("div");
    messageContent.textContent = message;

    // Create timestamp
    const messageTime = document.createElement("small");
    messageTime.className = "text-muted d-block mt-1";
    messageTime.textContent = timestamp || "Just now";

    // Assemble message
    messageDiv.appendChild(messageInfo);
    messageDiv.appendChild(messageContent);
    messageDiv.appendChild(messageTime);

    // Add to messages list
    messagesList.appendChild(messageDiv);

    // Scroll to bottom
    messagesList.scrollTop = messagesList.scrollHeight;
}

// Initialize when the document is ready
document.addEventListener("DOMContentLoaded", function () {
    console.log("DOM loaded, initializing SignalR and group chat");
    initializeSignalRConnection();
    initializeGroupChat(); // Initialize group chat functionality
});
