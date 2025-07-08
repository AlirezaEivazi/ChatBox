import React, { useState, useEffect, useRef } from 'react';
import { createChatConnection, ChatConnection } from './services/ChatService';
import './App.css';

interface Message {
  sender: string;
  content: string;
  timestamp: string;
}

const App: React.FC = () => {
  const [connection, setConnection] = useState<ChatConnection | null>(null);
  const [messages, setMessages] = useState<Message[]>([]);
  const [messageInput, setMessageInput] = useState('');
  const [connectionStatus, setConnectionStatus] = useState('Disconnected');
  const [roomId, setRoomId] = useState('general-room');
  const messagesEndRef = useRef<HTMLDivElement>(null);

  // Initialize connection
  useEffect(() => {
    const token = localStorage.getItem('token');
    if (!token) {
      console.error('No authentication token found');
      return;
    }

    const connect = async () => {
      try {
        const chatConnection = await createChatConnection(token, {
          onConnectionStatusChange: (status) => {
            setConnectionStatus(status);
          },
          onReconnecting: () => {
            console.log('Attempting to reconnect...');
          },
          onReconnected: () => {
            console.log('Reconnected successfully');
          }
        });

        chatConnection.on('ReceiveMessage', (message: Message) => {
          setMessages(prev => [...prev, message]);
        });

        chatConnection.on('UserConnected', (username: string) => {
          console.log(`${username} connected`);
        });

        chatConnection.on('UserDisconnected', (username: string) => {
          console.log(`${username} disconnected`);
        });

        setConnection(chatConnection);
        await chatConnection.invoke('JoinRoom', roomId);

      } catch (error) {
        console.error('Connection error:', error);
      }
    };

    connect();

    return () => {
      if (connection) {
        connection.off('ReceiveMessage');
        connection.off('UserConnected');
        connection.off('UserDisconnected');
        connection.stop();
      }
    };
  }, [roomId]);

  // Auto-scroll to bottom when messages change
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const sendMessage = async () => {
    if (!connection || !messageInput.trim()) return;

    try {
      await connection.invoke('SendMessage', roomId, messageInput);
      setMessageInput('');
    } catch (error) {
      console.error('Error sending message:', error);
    }
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      sendMessage();
    }
  };

  const changeRoom = (newRoomId: string) => {
    if (connection) {
      connection.invoke('LeaveRoom', roomId);
      setRoomId(newRoomId);
      setMessages([]);
    }
  };

  return (
    <div className="app-container">
      <div className="status-bar">
        Connection: <span className={`status-${connectionStatus.toLowerCase()}`}>
          {connectionStatus}
        </span> | Room: {roomId}
      </div>

      <div className="room-selector">
        <button onClick={() => changeRoom('general-room')}>General</button>
        <button onClick={() => changeRoom('random-room')}>Random</button>
        <button onClick={() => changeRoom('tech-room')}>Tech</button>
      </div>

      <div className="chat-container">
        <div className="messages-container">
          {messages.map((msg, i) => (
            <div key={i} className="message">
              <span className="sender">{msg.sender}</span>
              <span className="content">{msg.content}</span>
              <span className="timestamp">{msg.timestamp}</span>
            </div>
          ))}
          <div ref={messagesEndRef} />
        </div>

        <div className="input-area">
          <input
            type="text"
            value={messageInput}
            onChange={(e) => setMessageInput(e.target.value)}
            onKeyPress={handleKeyPress}
            placeholder="Type your message..."
            disabled={connectionStatus !== 'Connected'}
          />
          <button 
            onClick={sendMessage}
            disabled={!messageInput.trim() || connectionStatus !== 'Connected'}
          >
            Send
          </button>
        </div>
      </div>
    </div>
  );
};

export default App;