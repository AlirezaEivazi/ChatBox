import React, { useState, useEffect } from "react";
import * as signalR from "@microsoft/signalr";

interface Message {
  sender: string;
  content: string;
  timestamp: string;
}

const Chat = () => {
  const [messages, setMessages] = useState<Message[]>([]);
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  const [messageInput, setMessageInput] = useState("");
  const [roomId, setRoomId] = useState("general-room");

  // Initialize SignalR connection
  useEffect(() => {
    const startConnection = async (token: string) => {
      const newConnection = new signalR.HubConnectionBuilder()
        .withUrl("https://localhost:5001/chatHub", {
          accessTokenFactory: () => token
        })
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Information)
        .build();

      try {
        await newConnection.start();
        console.log("SignalR Connected");
        
        // Join the default room
        await newConnection.invoke("JoinRoom", roomId);

        // Setup message handler
        newConnection.on("ReceiveMessage", (message: Message) => {
          setMessages(prev => [...prev, message]);
        });

        newConnection.on("UserConnected", (username: string) => {
          console.log(`${username} connected`);
        });

        newConnection.on("UserDisconnected", (username: string) => {
          console.log(`${username} disconnected`);
        });

        setConnection(newConnection);
      } catch (err) {
        console.log("Connection failed: ", err);
      }
    };

    const token = localStorage.getItem("token");
    if (token) {
      startConnection(token);
    }

    return () => {
      if (connection) {
        connection.stop();
      }
    };
  }, []);

  const sendMessage = async () => {
    if (!connection || !messageInput.trim()) return;

    try {
      await connection.invoke("SendMessage", roomId, messageInput);
      setMessageInput("");
    } catch (err) {
      console.error("Error sending message: ", err);
    }
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === "Enter") {
      sendMessage();
    }
  };

  return (
    <div className="chat-container">
      <div className="messages-container">
        {messages.map((msg, i) => (
          <div key={i} className="message">
            <strong>{msg.sender}</strong>: {msg.content}
            <span className="timestamp">{msg.timestamp}</span>
          </div>
        ))}
      </div>
      <div className="input-area">
        <input
          type="text"
          value={messageInput}
          onChange={(e) => setMessageInput(e.target.value)}
          onKeyPress={handleKeyPress}
          placeholder="Type your message..."
        />
        <button onClick={sendMessage}>Send</button>
      </div>
    </div>
  );
};

export default Chat;
