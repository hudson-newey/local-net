using LocalNetNamespace;
using System.Net;
using System.Text;

using static Util;

public class Server
{
    public int serverPort = 8080;
    public string serverUrl = "localhost";
    public string searchEngine = "https://www.google.com/?q=";
    public string localCachePath = "./.cache/";
    public string queryParameter = "?q=";

    public Server()
    {
    }

    public void Start()
    {
        // create cache
        Util.CreateDirectory(this.localCachePath);

        // start server
        using var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:8080/");

        listener.Start();

        Console.WriteLine("Listening on port 8080...");

        while (true)
        {
            HttpListenerContext context = listener.GetContext();
            HttpListenerRequest req = context.Request;

            string query = this.extractQueryFromUrl(req.Url.ToString());

            using HttpListenerResponse resp = context.Response;

            // requests without a query
            if (query == "")
            {
                resp.Headers.Set("Content-Type", "text/plain");
                string respData = "Error 500: Internal Server Error";
                byte[] failedBuffer = Encoding.UTF8.GetBytes(respData);
                resp.ContentLength64 = failedBuffer.Length;

                using Stream failedRos = resp.OutputStream;
                failedRos.Write(failedBuffer, 0, failedBuffer.Length);
            }
            else
            {
                resp.Headers.Set("Content-Type", "text/html");
                Console.WriteLine(Environment.NewLine);
                Console.WriteLine($"With query {query}");

                string responseContent = this.FetchWebsiteContent(query);

                byte[] buffer = Encoding.UTF8.GetBytes(responseContent);
                resp.ContentLength64 = buffer.Length;

                using Stream ros = resp.OutputStream;
                ros.Write(buffer, 0, buffer.Length);
            }
        }
    }

    private string FetchWebsiteContent(string uri)
    {
        if (this.IsSearchTerm(uri))
        {
            uri = searchEngine + uri;
        }

        string queryKey = this.UriToSearchKey(uri);
        string localPath = this.CachePath(queryKey);

        Console.WriteLine($"Received request for {uri}");

        if (Util.FileExists(localPath))
        {
            Console.WriteLine($"Using cache for {uri}");
            return Util.ReadFile(localPath);
        }

        string htmlContent = "";
        using (WebClient client = new WebClient())
        {
            try
            {
                htmlContent = client.DownloadString(uri);
            }
            catch (System.Exception)
            {
                Console.WriteLine("Error: Fetching remote content");
            }
        }

        this.SaveContentToCache(uri, htmlContent);

        return htmlContent;
    }

    private void SaveContentToCache(string uri, string content)
    {
        string key = this.UriToSearchKey(uri);
        string localPath = this.CachePath(key);

        Util.WriteToFile(localPath, content);
    }

    private bool IsSearchTerm(string requestString)
    {
        // temp: we use :// because it denotes the end of a protocol in uri format
        return !requestString.Contains("://");
    }

    private string UriToSearchKey(string key)
    {
        key = key
            .Replace(":", "")
            .Replace("/", "s")
            .Replace("?", "q")
            .Replace("=", "e")
            .Replace(".", "d");
        return key;
    }

    private string CachePath(string key)
    {
        return $"{this.localCachePath}{key}.html";
    }

    private string extractQueryFromUrl(string url)
    {
        int indexOfQueryParameter = url.IndexOf(this.queryParameter) + this.queryParameter.Length;

        if (indexOfQueryParameter == -1)
        {
            return "";
        }

        return url.Substring(indexOfQueryParameter, url.Length - indexOfQueryParameter);
    }
}
