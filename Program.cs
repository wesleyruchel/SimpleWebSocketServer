using System.Net;
using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5001");

var app = builder.Build();

app.UseWebSockets();

app.Map("/", async context =>
{

    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
    }
    else
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

        while (true)
        {
            var message = "The current time is: " + DateTime.Now.ToString("HH:mm:ss");
            var bytes = Encoding.UTF8.GetBytes(message);
            var arraySegment = new ArraySegment<byte>(bytes, 0, bytes.Length);

            if (webSocket.State == WebSocketState.Open)
            {
                await webSocket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            else if (webSocket.State == WebSocketState.Closed || webSocket.State == WebSocketState.Aborted)
            {
                break;
            }
            Thread.Sleep(1000);

        }
    }

});

await app.RunAsync();
