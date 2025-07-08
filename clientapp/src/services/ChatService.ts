import * as signalR from "@microsoft/signalr";
import { toast } from "react-toastify"; // Optional for notifications

// Configuration interface
interface ConnectionOptions {
  onConnectionStatusChange?: (status: string) => void;
  onReconnecting?: (err?: Error) => void;
  onReconnected?: (connectionId?: string) => void;
}

export class ChatConnection {
  private connection: signalR.HubConnection;
  private token: string;
  private options?: ConnectionOptions;

  constructor(token: string, options?: ConnectionOptions) {
    this.token = token;
    this.options = options;
    this.connection = this.createConnection();
    this.setupEventHandlers();
  }

  private createConnection(): signalR.HubConnection {
    return new signalR.HubConnectionBuilder()
      .withUrl("https://localhost:7138/chatHub", {
        accessTokenFactory: () => this.token,
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          if (retryContext.elapsedMilliseconds < 60000) {
            // Retry every 2 seconds for the first minute
            return 2000;
          }
          // After 1 minute, retry every 10 seconds
          return 10000;
        }
      })
      .configureLogging(signalR.LogLevel.Warning)
      .build();
  }

  private setupEventHandlers(): void {
    this.connection.onclose((err) => {
      this.options?.onConnectionStatusChange?.("Disconnected");
      toast.error("Connection lost. Attempting to reconnect...");
    });

    this.connection.onreconnecting((err) => {
      this.options?.onConnectionStatusChange?.("Reconnecting");
      this.options?.onReconnecting?.(err);
    });

    this.connection.onreconnected((connectionId) => {
      this.options?.onConnectionStatusChange?.("Connected");
      this.options?.onReconnected?.(connectionId);
      toast.success("Connection restored");
    });
  }

  public async start(): Promise<void> {
    try {
      await this.connection.start();
      this.options?.onConnectionStatusChange?.("Connected");
    } catch (err) {
      console.error("SignalR Connection Error:", err);
      this.options?.onConnectionStatusChange?.("Failed");
      throw new Error("Failed to start connection");
    }
  }

  public async stop(): Promise<void> {
    try {
      await this.connection.stop();
      this.options?.onConnectionStatusChange?.("Disconnected");
    } catch (err) {
      console.error("Error while stopping connection:", err);
    }
  }

  public on(methodName: string, callback: (...args: any[]) => void): void {
    this.connection.on(methodName, callback);
  }

public off(methodName: string, callback?: (...args: any[]) => void): void {
    if (callback) {
        this.connection.off(methodName, callback);
    } else {
        // Remove all handlers for this method if no callback provided
        this.connection.off(methodName);
    }
}

  public async invoke<T>(methodName: string, ...args: any[]): Promise<T> {
    if (this.connection.state !== signalR.HubConnectionState.Connected) {
      throw new Error("Connection is not active");
    }
    return this.connection.invoke<T>(methodName, ...args);
  }

  public get state(): signalR.HubConnectionState {
    return this.connection.state;
  }
}

// Utility function for quick setup
export const createChatConnection = async (
  token: string,
  options?: ConnectionOptions
): Promise<ChatConnection> => {
  const connection = new ChatConnection(token, options);
  await connection.start();
  return connection;
};