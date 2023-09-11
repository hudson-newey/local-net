using LocalNetNamespace;
using System.Net;
using System.Text;

using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;

using static Util;

public class Server
{
    public int serverPort = 8080;
    public string serverUrl = "localhost";
    public string searchEngine = "https://html.duckduckgo.com/html/?q=";
    public string localCachePath = "./.cache/";
    public string queryParameter = "?q=";
    public string adminConsoleSubPath = "/admin";

    public Server()
    {
    }

    public string interceptorPath()
    {
        return $"http://{this.serverUrl}:{this.serverPort}/{this.queryParameter}";
    }

    public string adminConsolePath()
    {
        return $"http://{this.serverUrl}:{this.serverPort}{this.adminConsoleSubPath}";
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

            string query = this.ExtractQueryFromUrl(req.Url.ToString());
            query = this.CreateUrl(query);

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
                string responseMimeType = this.MimeType(query);

                resp.Headers.Set("Content-Type", responseMimeType);
                Console.WriteLine(Environment.NewLine);
                Console.WriteLine($"With query {query}");

                if (this.IsImage(query))
                {
                    byte[] buffer = this.FetchImageContent(query);

                    resp.ContentLength64 = buffer.Length;

                    using Stream ros = resp.OutputStream;
                    ros.Write(buffer, 0, buffer.Length);
                }
                else
                {
                    string responseContent = this.FetchWebsiteContent(query);

                    byte[] buffer = Encoding.UTF8.GetBytes(responseContent);
                    resp.ContentLength64 = buffer.Length;

                    using Stream ros = resp.OutputStream;
                    ros.Write(buffer, 0, buffer.Length);
                }
            }
        }
    }

    private byte[] FetchImageContent(string uri)
    {
        byte[] imageContent = { };

        Console.WriteLine($"Received request for image {uri}");

        string imageFileExtension = this.ImageExtension(uri);
        string queryKey = this.UriToSearchKey(uri);
        string localPath = this.CachePath(queryKey) + imageFileExtension;

        if (Util.FileExists(localPath))
        {
            Console.WriteLine($"Using cache for image {uri}");
            return Util.ReadFileBytes(localPath);
        }

        using (WebClient client = new WebClient())
        {
            try
            {
                Console.WriteLine($"Fetching remote image content for {uri}");
                imageContent = client.DownloadData(uri);
            }
            catch (System.Exception e)
            {
                Console.WriteLine("Error: Fetching image remote content");
                Console.WriteLine(e);
            }
        }

        this.SaveImageToCache(uri, imageContent);

        return imageContent;
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
                Console.WriteLine($"Fetching remote content for {uri}");

                htmlContent = client.DownloadString(uri);
                htmlContent = this.ReplaceLinks(uri, htmlContent);
            }
            catch (System.Exception e)
            {
                Console.WriteLine("Error: Fetching remote content");
                Console.WriteLine(e);
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

    private string ImageExtension(string uri)
    {
        string fileExtension = ".png";

        if (uri.EndsWith(".jpg"))
        {
            fileExtension = ".jpg";
        }
        else if (uri.EndsWith(".jpeg"))
        {
            fileExtension = ".jpeg";
        }
        else if (uri.EndsWith(".gif"))
        {
            fileExtension = ".gif";
        }
        else if (uri.EndsWith(".ico"))
        {
            fileExtension = ".ico";
        }

        return fileExtension;
    }

    private void SaveImageToCache(string uri, byte[] content)
    {
        string fileExtension = this.ImageExtension(uri);
        string key = this.UriToSearchKey(uri);
        string cachePathKey = this.CachePath(key) + fileExtension;
        Util.WriteImageToFile(cachePathKey, content);
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
            .Replace("/", "")
            .Replace("?", "")
            .Replace("=", "")
            .Replace(".", "");
        return key;
    }

    private string CachePath(string key)
    {
        return $"{this.localCachePath}{key}.html";
    }

    private string ExtractQueryFromUrl(string url)
    {
        int indexOfQueryParameter = url.IndexOf(this.queryParameter) + this.queryParameter.Length;

        if (indexOfQueryParameter == -1)
        {
            return "";
        }

        return url.Substring(indexOfQueryParameter, url.Length - indexOfQueryParameter);
    }

    private string ReplaceLinks(string urlPath, string contents)
    {
        string resultContent = contents;
        string baseUrl = this.ExtractBaseUrl(urlPath);

        List<string> htmlRemoteElements = this.ExtractRemoteAttributes(contents);

        foreach (string item in htmlRemoteElements)
        {
            bool isBasePath = item.StartsWith("/");

            if (!this.isInterceptorPath(item) && !this.IsAbsolutePath(item))
            {
                if (isBasePath)
                {
                    resultContent = resultContent.Replace($"src=\"{item}\"", $"src={this.interceptorPath() + baseUrl + item}\"");
                    resultContent = resultContent.Replace($"href=\"{item}\"", $"href={this.interceptorPath() + baseUrl + item}\"");
                }
                else
                {
                    // this is not needed for the interceptor
                    string newItem = item.Replace("./", "");
                    resultContent = resultContent.Replace($"src=\"{item}\"", $"src={this.interceptorPath() + urlPath + newItem}\"");
                    resultContent = resultContent.Replace($"href=\"{item}\"", $"href={this.interceptorPath() + urlPath + newItem}\"");
                }
            }
            else if (this.IsAbsolutePath(item))
            {
                resultContent = resultContent.Replace($"src=\"{item}\"", $"src=\"{this.interceptorPath() + item}\"");
                resultContent = resultContent.Replace($"href=\"{item}\"", $"href=\"{this.interceptorPath() + item}\"");
            }
        }

        return resultContent;
    }

    private bool IsAbsolutePath(string url)
    {
        return url.Contains("://");
    }

    private List<string> ExtractRemoteAttributes(string html)
    {
        List<string> result = new List<string>();

        // Load the HTML document using HtmlAgilityPack
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Select all elements with src or href attributes
        var elementsWithAttributes = doc.DocumentNode.Descendants()
            .Where(node => node.Attributes["src"] != null || node.Attributes["href"] != null);

        // Extract and add the values of src and href attributes to the result list
        foreach (var element in elementsWithAttributes)
        {
            if (element.Attributes["src"] != null)
            {
                string srcValue = element.Attributes["src"].Value;
                result.Add(srcValue);
            }

            if (element.Attributes["href"] != null)
            {
                string hrefValue = element.Attributes["href"].Value;
                result.Add(hrefValue);
            }
        }

        return result;
    }

    private string ExtractBaseUrl(string url)
    {
        Uri uri = new Uri(url);
        return uri.Host;
    }

    private bool isInterceptorPath(string url)
    {
        return url.Contains("localhost:8080") || url == "";
    }

    // some absolute urls do not use http at the start
    private string CreateUrl(string url)
    {
        string resultUrl = url;
        resultUrl = resultUrl.Replace("\"", "");

        if (!this.IsAbsolutePath(resultUrl) &&
            (
                resultUrl.Contains(".com") ||
                resultUrl.Contains(".net") ||
                resultUrl.Contains(".org") ||
                resultUrl.Contains(".io")
            )
        )
        {
            return "http://" + resultUrl;
        }

        return resultUrl;
    }

    private string MimeType(string url)
    {
        string mimeType = "text/html";

        if (url.EndsWith(".css"))
        {
            mimeType = "text/css";
        }

        if (url.EndsWith(".js"))
        {
            mimeType = "text/javascript";
        }

        return mimeType;
    }

    private bool IsImage(string url)
    {
        return url.EndsWith(".png") || url.EndsWith(".jpg") || url.EndsWith(".jpeg") || url.EndsWith(".gif") || url.EndsWith(".ico");
    }
}
