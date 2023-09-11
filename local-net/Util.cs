using System.IO;
using System;


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

    public static void WriteImageToFile(string filePath, byte[] content)
    {
        try
        {
            File.WriteAllBytes(filePath, content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing the image: {ex.Message}");
        }
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
