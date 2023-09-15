using LocalNetNamespace;

using System.Net;
using System.Text;

using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;

using static AdminConsole;
using static Util;
using static Statistics;

public class Server
{
    public int serverPort = 8080;
    public string serverUrl = "localhost";
    public string searchEngine = "https://html.duckduckgo.com/html/?q=";
    public string localCachePath = "./.cache/";
    public string queryParameter = "?interceptor-url=";

    public Server()
    {
    }

    public string interceptorPath()
    {
        return $"http://{this.serverUrl}:{this.serverPort}/{this.queryParameter}";
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

            string requestPath = req.Url.AbsolutePath;

            // handle requests for the admin console
            if (requestPath == AdminConsole.adminConsoleSubpath)
            {
                resp.Headers.Set("Content-Type", "text/html");
                byte[] buffer = Encoding.UTF8.GetBytes(AdminConsole.render(this));
                resp.ContentLength64 = buffer.Length;

                using Stream ros = resp.OutputStream;
                ros.Write(buffer, 0, buffer.Length);
                continue;
            }

            // requests without a query
            if (query == "")
            {
                // handle requests for the root page
                if (requestPath == RootPage.rootPageSubpath)
                {
                    resp.Headers.Set("Content-Type", "text/html");
                    byte[] buffer = Encoding.UTF8.GetBytes(RootPage.render());
                    resp.ContentLength64 = buffer.Length;

                    using Stream ros = resp.OutputStream;
                    ros.Write(buffer, 0, buffer.Length);
                    continue;
                }

                resp.StatusCode = 500;
                resp.Close();
            }
            else
            {
                Statistics.totalRequestsCount++;

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
                    byte[] responseContent = this.FetchWebsiteContent(query);

                    byte[] buffer = responseContent;
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

            Statistics.cachedRequestsCount++;

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

    private byte[] FetchWebsiteContent(string uri)
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
            Statistics.cachedRequestsCount++;
            return Util.ReadFileBytes(localPath);
        }

        byte[] htmlContent = { };

        using (WebClient client = new WebClient())
        {
            try
            {
                Console.WriteLine($"Fetching remote content for {uri}");

                htmlContent = client.DownloadData(uri);
                if (this.IsImage(uri) || !Util.IsStringValue(htmlContent))
                {
                    this.SaveImageToCache(uri, htmlContent);
                }
                else
                {
                    string htmlString = Encoding.UTF8.GetString(htmlContent);
                    htmlString = this.ReplaceLinks(uri, htmlString);
                    htmlContent = Encoding.UTF8.GetBytes(htmlString);
                    this.SaveContentToCache(uri, htmlString);
                }
            }
            catch (System.Exception e)
            {
                Console.WriteLine("Error: Fetching remote content");
                Console.WriteLine(e);
            }
        }

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
        int indexOfQueryParameter = url.IndexOf(this.queryParameter);

        if (indexOfQueryParameter == -1)
        {
            return "";
        }

        indexOfQueryParameter += this.queryParameter.Length;

        return url.Substring(indexOfQueryParameter, url.Length - indexOfQueryParameter);
    }

    private string ReplaceLinks(string urlPath, string contents)
    {
        string resultContent = contents;
        string baseUrl = this.ExtractBaseUrl(urlPath);
        string subPath = this.ExtractSubPath(urlPath);

        List<string> htmlRemoteElements = this.ExtractRemoteAttributes(contents);

        foreach (string item in htmlRemoteElements)
        {
            bool isBasePath = item.StartsWith("/");

            if (!this.isInterceptorPath(item) && !this.IsAbsolutePath(item))
            {
                if (isBasePath)
                {
                    resultContent = resultContent.Replace($"src=\"{item}\"", $"src=\"{this.interceptorPath() + baseUrl + item}\"");
                    resultContent = resultContent.Replace($"href=\"{item}\"", $"href=\"{this.interceptorPath() + baseUrl + item}\"");
                }
                else
                {
                    // this is not needed for the interceptor
                    string newItem = item.Replace("./", "");
                    resultContent = resultContent.Replace($"src=\"{item}\"", $"src=\"{this.interceptorPath() + baseUrl + subPath + newItem}\"");
                    resultContent = resultContent.Replace($"href=\"{item}\"", $"href=\"{this.interceptorPath() + baseUrl + subPath + newItem}\"");
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
        return url.Contains("//");
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
        string uriHost = uri.Host;
        return uriHost;
    }

    private string ExtractSubPath(string url)
    {
        Uri uri = new Uri(url);
        string uriPath = uri.AbsolutePath;
        string[] uriPathParts = uriPath.Split("/");
        string lastPathPart = uriPathParts[uriPathParts.Length - 1];

        // remove the last part of the path
        uriPathParts = uriPathParts.Take(uriPathParts.Count() - 1).ToArray();

        string result = string.Join("/", uriPathParts);

        if (!result.EndsWith("/"))
        {
            result = result + "/";
        }

        return result;
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

        string[] topLevelDomains = {
            ".com",
            ".net",
            ".org",
            ".io",
            ".de",
            ".co.uk",
            ".co",
            ".us",
            ".ca",
            ".biz",
            ".info",
            ".me",
            ".mobi",
            ".tv",
            ".ws",
            ".name",
            ".cc",
            ".jp",
            ".be",
            ".at",
            ".au",
            ".in",
            ".uk",
            ".tk",
            ".nz",
            ".ru",
            ".fr",
            ".ch",
            ".it",
            ".nl",
            ".se",
            ".no",
            ".es",
            ".mil",
            ".edu",
            ".gov",
            ".kr",
            ".cn",
            ".tw",
            ".sg",
            ".hk",
            ".my",
            ".xyz",
            ".top",
            ".club",
            ".vip",
            ".win",
            ".site",
            ".bid",
        };

        bool hasTld = topLevelDomains.Any(tld => resultUrl.EndsWith(tld));

        if (resultUrl == "")
        {
            return "";
        }

        if (!this.IsAbsolutePath(resultUrl))
        {
            return "http://" + resultUrl;
        }
        else
        {
            if (resultUrl.StartsWith("//"))
            {
                resultUrl = "http:" + resultUrl;
            }
        }

        return resultUrl;
    }

    private string MimeType(string url)
    {
        string mimeType = "text/html";

        switch (Path.GetExtension(url))
        {
            case ".png":
                mimeType = "image/png";
                break;
            case ".jpg":
                mimeType = "image/jpg";
                break;
            case ".jpeg":
                mimeType = "image/jpeg";
                break;
            case ".gif":
                mimeType = "image/gif";
                break;
            case ".ico":
                mimeType = "image/ico";
                break;
            case ".svg":
                mimeType = "image/svg+xml";
                break;
            case ".css":
                mimeType = "text/css";
                break;
            case ".js":
                mimeType = "text/javascript";
                break;
            case ".woff2":
                mimeType = "font/woff2";
                break;
            case ".woff":
                mimeType = "font/woff";
                break;
            case ".ttf":
                mimeType = "font/ttf";
                break;
        }

        return mimeType;
    }

    private bool IsImage(string url)
    {
        return url.EndsWith(".png") || url.EndsWith(".jpg") || url.EndsWith(".jpeg") || url.EndsWith(".gif") || url.EndsWith(".ico") || url.EndsWith(".svg");
    }
}
