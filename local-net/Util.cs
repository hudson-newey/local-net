using System.IO;


static class Util
{
    public static string ReadFile(string filePath)
    {
        return File.ReadAllText(@$"{filePath}");
    }

    public static void WriteToFile(string filePath, string content)
    {
        File.WriteAllText(filePath, content);
    }

    public static bool FileExists(string filePath)
    {
        return File.Exists(filePath);
    }

    public static void CreateDirectory(string path)
    {
        if(!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}
