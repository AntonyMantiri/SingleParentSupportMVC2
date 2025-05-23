"use strict";

// Get the current user ID from a data attribute
const currentUserId = document.querySelector('meta[name="user-id"]')?.content;
let selectedUserId = null;
let connection = null;

// Initialize the SignalR connection
function initializeSignalRConnection() {
    // Create the connection
    connection = new signalR.HubConnectionBuilder()
        .withUrl("/chatHub")
        .withAutomaticReconnect()
        .build();

    // Handle receiving messages
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
        connection.invoke("SendMessage", message, selectedUserId)
            .catch(err => console.error(err));

        messageInput.value = "";
    }

    // Focus back on the input
    messageInput.focus();
}

// Select a user to chat with
function selectUser(userItem) {
    // Remove active class from all users
    document.querySelectorAll(".chat-user").forEach(item => {
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

// Display a message in the chat
function displayMessage(senderId, senderName, message, senderRole) {
    const messagesList = document.getElementById("messagesList");

    // Create message element
    const messageDiv = document.createElement("div");
    messageDiv.className = senderId === currentUserId ? "message message-sent" : "message message-received";

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

// Initialize when the document is ready
document.addEventListener("DOMContentLoaded", function () {
    initializeSignalRConnection();
});
