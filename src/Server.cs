using System.Net;
using System.Net.Sockets;
using System.Text;

internal class Program
{
    const string SUCCESS_200 = "HTTP/1.1 200 OK";
    const string NOT_FOUND_404 = "HTTP/1.1 404 Not Found";
    const string SECTION_BREAK = "\r\n\r\n";
    const string LINE_BREAK = "\r\n";

    private static async Task Main(string[] args)
    {
        Console.WriteLine("App is started...");

        bool runServer = true;
        var server = new TcpListener(IPAddress.Any, 8080);
        server.Start();

        while (runServer)
        {
            var client = await server.AcceptTcpClientAsync();

            Console.WriteLine("LINE 16: Client connected...");

            _ = Task.Run(async () => await HandleClientAsync(client));
        }

        server.Stop();
        Console.WriteLine("App is stopped...");
    }

    private static async Task HandleClientAsync(TcpClient client)
    {
        using (client)
        {
            NetworkStream clientStream = client.GetStream();
            var content = await ReadStringContentFromNetworkStream(clientStream);
            Console.WriteLine("\n\n" + content + "\n\n");
            var requestTarget = GetRequestTarget(content);

            if (requestTarget == "/")
            {
                await WriteContentToNetworkStream(clientStream,
                    $"{SUCCESS_200}{LINE_BREAK}" +
                    $"Content-Type: text/plain{LINE_BREAK}");
            }
            else if (requestTarget.StartsWith("/echo/"))
            {
                string echoContent = Uri.UnescapeDataString(requestTarget.Substring("/echo/".Length));

                await WriteContentToNetworkStream(clientStream,
                    $"{SUCCESS_200}{LINE_BREAK}" +
                    $"Content-Type: text/plain{LINE_BREAK}" +
                    $"Content-Length: {echoContent.Length}{SECTION_BREAK}" +
                    $"{echoContent}");
            }
            else if (requestTarget == "/user-agent")
            {
                string userAgent = GetHeader(content, "User-Agent");
                await WriteContentToNetworkStream(clientStream,
                    $"{SUCCESS_200}{LINE_BREAK}" +
                    $"Content-Type: text/plain{LINE_BREAK}" +
                    $"Content-Length: {userAgent.Length}{SECTION_BREAK}" +
                    $"{userAgent}");
            }
            else
            {
                await WriteContentToNetworkStream(clientStream, NOT_FOUND_404 + LINE_BREAK);
            }

            clientStream.Close();
        }
    }

    private static string GetHeader(string content, string headerName)
    {
        var splittedContent = SplitHttpRequest(content);
        var result = splittedContent.FirstOrDefault(c => c.StartsWith(headerName)) ?? string.Empty;

        if (result == string.Empty)
        {
            return result;
        }

        return result.Substring(headerName.Length + 2);
    }

    private static async Task<string> GetHeaderAsync(NetworkStream stream, string headerName)
    {
        var content = await ReadStringContentFromNetworkStream(stream);
        return GetHeader(content, headerName);
    }

    static Task WriteContentToNetworkStream(NetworkStream stream, string content)
    {
        return stream.WriteAsync(Encoding.UTF8.GetBytes(content), 0, content.Length);
    }

    static async Task<string> ReadStringContentFromNetworkStream(NetworkStream stream)
    {
        var buffer = new byte[1024];
        await stream.ReadAsync(buffer, 0, buffer.Length);

        return Encoding.UTF8.GetString(buffer);
    }

    static string[] SplitHttpRequest(string request, string splitter = "\r\n")
    {
        return request.Split(splitter);
    }

    static string GetRequestTarget(string requestString)
    {
        var requestLine = SplitHttpRequest(requestString)[0];

        var requestParts = requestLine.Split(" ");
        if (requestParts.Length < 2)
        {
            return string.Empty;
        }

        return requestParts[1];
    }
}