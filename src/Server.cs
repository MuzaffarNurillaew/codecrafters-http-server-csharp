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

            using NetworkStream clientStream = client.GetStream();
            var content = await ReadStringContentFromNetworkStream(clientStream);
            var requestTarget = GetRequestTarget(content);

            if (requestTarget == "/")
            {
                // Status line
                await WriteContentToNetworkStream(clientStream, SUCCESS_200);
                await WriteContentToNetworkStream(clientStream, LINE_BREAK);


                // Headers
                await WriteContentToNetworkStream(clientStream, "Content-Type: text/plain");
                await WriteContentToNetworkStream(clientStream, LINE_BREAK);
            }
            else if (requestTarget.StartsWith("/echo/"))
            {
                string echoContent = Uri.UnescapeDataString(requestTarget.Substring("/echo/".Length));

                // Status line
                await WriteContentToNetworkStream(clientStream, SUCCESS_200);
                await WriteContentToNetworkStream(clientStream, LINE_BREAK);

                // Headers
                await WriteContentToNetworkStream(clientStream, "Content-Type: text/plain");
                await WriteContentToNetworkStream(clientStream, LINE_BREAK);
                await WriteContentToNetworkStream(clientStream, $"Content-Length: {echoContent.Length}");
                await WriteContentToNetworkStream(clientStream, SECTION_BREAK);

                // Response body
                await WriteContentToNetworkStream(clientStream, echoContent);
            }
            else
            {
                await WriteContentToNetworkStream(clientStream, NOT_FOUND_404);
            }

            client.Close();
        }

        server.Stop();
        Console.WriteLine("App is stopped...");
    }

    static Task WriteContentToNetworkStream(NetworkStream stream, string content)
    {
        return stream.WriteAsync(Encoding.UTF8.GetBytes(content), 0, content.Length);
    }

    static async Task<string> ReadStringContentFromNetworkStream(NetworkStream stream)
    {
        var buffer = new byte[1024];
        await stream.ReadAsync(buffer);

        return Encoding.UTF8.GetString(buffer);
    }

    static string[] SplitHttpRequest(string request, string splitter = "\r\n")
    {
        return request.Split(splitter);
    }

    static string GetRequestTarget(string requestString)
    {
        try
        {
            var requestLine = SplitHttpRequest(requestString)[0];

            var result = requestLine.Split(" ")[1];

            return result;
        }
        catch (IndexOutOfRangeException)
        {
            Console.WriteLine($"ERR: {nameof(IndexOutOfRangeException)}, request string: {requestString}");
            return string.Empty;
        }
    }
}