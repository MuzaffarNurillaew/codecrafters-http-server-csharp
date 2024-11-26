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
            string response = GetResponse(404, [], "");

            if (requestTarget == "/")
            {
                response = GetResponse(
                    statusCode: 200,
                    headers: new Dictionary<string, string> { ["Content-Type"] = "text/plain" },
                    body: "");
            }
            else if (requestTarget.StartsWith("/echo/"))
            {
                string echoContent = Uri.UnescapeDataString(requestTarget.Substring("/echo/".Length));

                response = GetResponse(
                    statusCode: 200,
                    headers: new Dictionary<string, string> { ["Content-Type"] = "text/plain", ["Content-Length"] = echoContent.Length.ToString() },
                    body: echoContent);
            }
            else if (requestTarget.StartsWith("/files/"))
            {
                string fileName = Uri.UnescapeDataString(requestTarget.Substring("/files/".Length));
                string fileContent = "";
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\src\\wwwroot\\", fileName);
                if (File.Exists(filePath))
                {
                    fileContent = File.ReadAllText(filePath);
                    response = GetResponse(
                        statusCode: 200,
                        headers: new Dictionary<string, string> { ["Content-Type"] = "application/octet-stream", ["Content-Length"] = fileContent.Length.ToString() },
                        body: fileContent);
                }
                else
                {
                    response = GetResponse(
                        statusCode: 404,
                        headers: [],
                        body: fileContent);
                }

            }
            else if (requestTarget == "/user-agent")
            {
                string userAgent = GetHeader(content, "User-Agent");
                response = GetResponse(
                    statusCode: 200,
                    headers: new Dictionary<string, string> { ["Content-Type"] = "text/plain", ["Content-Length"] = userAgent.Length.ToString() },
                    body: userAgent);
            }

            await WriteContentToNetworkStream(clientStream, response);
            clientStream.Close();
        }
    }

    private static string GetResponse(int statusCode, Dictionary<string, string> headers, string body)
    {
        var sb = new StringBuilder();
        sb.Append($"HTTP/1.1 {statusCode}{LINE_BREAK}");

        string headersText = GenerateHeaderString(headers);
        sb.Append(headersText + LINE_BREAK);

        sb.Append(body);

        return sb.ToString();
    }

    private static string GenerateHeaderString(Dictionary<string, string> headers)
    {
        var sb = new StringBuilder();
        foreach (var header in headers)
        {
            sb.Append($"{header.Key}: {header.Value}{LINE_BREAK}");
        }

        return sb.ToString();
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