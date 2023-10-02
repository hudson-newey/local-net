using LocalNetNamespace;

using System.IO;
using System;

static class Util
{
    public static string ReadFile(string filePath)
    {
        return File.ReadAllText(@$"{filePath}");
    }

    public static byte[] ReadFileBytes(string filePath)
    {
        return File.ReadAllBytes(filePath);
    }

    public static string[] ReadFileLines(string filePath)
    {
        return File.ReadAllLines(filePath);
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

    public static int DirectoryFilesCount(string path)
    {
        return Directory.GetFiles(path).Length;
    }

    // if the data is a binary object, return false
    // if the data is a string object, return true
    public static bool IsStringValue(object data)
    {
        return true;
    }
}
