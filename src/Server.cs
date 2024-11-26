using System.Net;
using System.Text;

internal class Program
{
    private static bool runServer = true;
    public static string pageData =
            "<!DOCTYPE>" +
            "<html>" +
            "  <head>" +
            "    <title>HttpListener Example</title>" +
            "  </head>" +
            "  <body>" +
            "    <p>Page Views: {0}</p>" +
            "    <form method=\"post\" action=\"shutdown\">" +
            "      <input type=\"submit\" value=\"Shutdown\" {1}>" +
            "    </form>" +
            "<script>console.log('Muzaffar')</script>" +
            "  </body>" +
            "</html>";
    private static async Task Main(string[] args)
    {
        Console.WriteLine("App started");

        // create http listener
        HttpListener listener = new HttpListener();

        // configure listener
        listener.Prefixes.Add("http://localhost:8080/");
        listener.Start();

        await HandleConnections();


        listener.Close();


        async Task HandleConnections()
        {
            while (runServer)
            {
                var context = await listener.GetContextAsync();
                var outputStream = context.Response.OutputStream;

                var request = context.Request;

                var response = context.Response;

                Console.WriteLine(request.Url.AbsolutePath + (request.Url.AbsolutePath.Length == "/index.html".Length));
                if (request.HttpMethod == HttpMethod.Get.Method && string.Equals(request.Url.AbsolutePath, "/index.html", StringComparison.OrdinalIgnoreCase))
                {
                    response.StatusCode = 200;
                    response.ContentType = "text/html";
                    var bytes = Encoding.UTF8.GetBytes(string.Format(pageData, 0, ""));
                }
                else
                {
                    response.StatusCode = 404;
                    response.ContentType = "text/html";
                    var bytes = Encoding.UTF8.GetBytes("<h1>Not found</h1>");
                }

                response.Close();
            }
        }
    }
}