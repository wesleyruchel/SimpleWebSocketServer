using System.Net;
using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5001");

var app = builder.Build();

app.UseWebSockets();

var connections = new List<WebSocket>();

app.Map("/", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
    }
    else
    {
        var username = context.Request.Query["username"];
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        connections.Add(webSocket);

        await Broadcast($"{username} joined");
        await Broadcast($"{connections.Count} users connected");
        await ReceiveMessage(webSocket, async (receive, buffer) =>
        {
            if (receive.MessageType == WebSocketMessageType.Text)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, receive.Count);

                await Broadcast(username + ": " + message);
            }
            else if (receive.MessageType == WebSocketMessageType.Close || webSocket.State == WebSocketState.Aborted)
            {
                connections.Remove(webSocket);

                await Broadcast($"{username} left");
                await Broadcast($"{connections.Count} users connected");
                await webSocket.CloseAsync(receive.CloseStatus.Value, receive.CloseStatusDescription, CancellationToken.None);
            }
        });
    }
});

async Task ReceiveMessage(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage)
{
    var buffer = new byte[1024 * 6];

    while (socket.State == WebSocketState.Open)
    {
        var receive = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        handleMessage(receive, buffer);
    }
}

async Task Broadcast(string message)
{
    var bytes = Encoding.UTF8.GetBytes(message);

    foreach (var socket in connections)
    {
        if (socket.State == WebSocketState.Open)
        {
            var arraySegment = new ArraySegment<byte>(bytes, 0, bytes.Length);

            await socket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}

await app.RunAsync();
