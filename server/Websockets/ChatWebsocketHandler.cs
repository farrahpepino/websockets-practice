namespace server.Websockets
{
    /*
        Dictionary → keeps track of connected users.

        HandleAsync → accepts connection, saves user.

        ReceiveMessagesAsync → listens for messages.

        SendMessageAsync → sends messages to a user.
    */

    public static class ChatWebsocketHandler{
        private static readonly ConcurrentDictionary<string, Websocket> _connections = new();
        public static async Task HandleAsync(HttpContext context){
            /*
                Check if websocket request
                getuser id
                accept connection
                add to dictionary
                call receiving func

            */

            if (context.WebSockets.IsWebSocketRequest){ 
                var userId = context.Request.Query["userId"];
                if(string.IsNullOrEmpty(userId)){ 
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Missing userId");
                    return;
                }
                using var websocket = await context.WebSockets.AcceptWebSocketAsync();
                _connections[userId]= websocket; 
                Console.WriteLine($`WebSocket connected: {userId}`);
            
                await ReceiveMessagesAsync(userId, websocket);

            }
            else{
                context.Response.StatusCode = 400;
            }
        }

        public static async Task ReceiveMessagesAsync(string userid, WebSocket socket){
            var buffer = new byte[1024*4];
            while(socket.State == WebSocketState.Open){
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close){
                    _connections.TryRemove(userid, out _);
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by user", CancellationToken.None);
                    Console.WriteLine($"Websocket closed: {userId}");
                }
            }
        }

        public static async Task<bool> SendMessageToUserAsync(string userId, string message){
            if (string.IsNullOrEmpty(userId) || !_connections.TryGetValue(userId, out var socket))
                return false;

            if (socket.State == WebSocketState.Close){
                _connections.TryRemove(userId, out _);
                return false;
            }

            try
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                await socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
                return true;
            }
            catch
            {
                _connections.TryRemove(userId, out _);
                return false;
            }
        }

    
        public static IReadOnlyDictionary<string, WebSocket> GetConnections(){
            return _connections;
        }
    }
}