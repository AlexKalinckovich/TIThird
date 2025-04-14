using System.IO;
using Microsoft.Win32;
using ArgumentNullException = System.ArgumentNullException;

namespace TIThird.Utils.FileClasses;

public class FileManager
{
    public static string GetFilePath(FileType fileType, string prefix = "")
    {
        try
        {
            return fileType == FileType.InputFile ? GetInputFilePath() : GetOutputFilePath(prefix);
        }
        catch (Exception ex)
        {
            throw new FileOperationException("File operation failed", ex);
        }
    }
    
    private static string GetInputFilePath()
    {
        FileDialog dialog = new OpenFileDialog();
        return dialog.ShowDialog() == true ? ValidateInputPath(dialog.FileName) : string.Empty;
    }

    private static string GetOutputFilePath(string prefix)
    {
        FileDialog dialog = new SaveFileDialog();
        if(dialog.ShowDialog() != true) return string.Empty;
        
        string outputFilePath = GenerateOutputFilePath(dialog.FileName, prefix);
        EnsureOutputDirectoryExists(outputFilePath);
        CreateEmptyFileIfNeeded(outputFilePath);
        return outputFilePath;
    }

    private static string GenerateOutputFilePath(string basePath, string prefix)
    {
        if (string.IsNullOrWhiteSpace(basePath))
        {
            throw new ArgumentException("Invalid file path");
        }
        
        return Path.Combine(Path.GetDirectoryName(basePath) ?? Directory.GetCurrentDirectory(), 
                            $"{prefix}{Path.GetFileName(basePath)}");
    }

    private static void EnsureOutputDirectoryExists(string path)
    {
        string directory = Path.GetDirectoryName(path)!;
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static void CreateEmptyFileIfNeeded(string path)
    {
        if (!File.Exists(path))
        {
            File.Create(path).Dispose();
        }
    }

    private static string ValidateInputPath(string inputPath)
    {
        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException("File not found", inputPath);
        }

        if (new FileInfo(inputPath).Length == 0)
        {
            throw new InvalidDataException("File is empty");
        }
        
        return inputPath;
    }
    
    private static string GetFullPath(string filePath, string prefix = "")
    {
        
        if(string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
        
        return Path.Combine(Path.GetDirectoryName(filePath), prefix + Path.GetFileName(filePath));
    }
}

public enum FileType { InputFile, OutputFile }

public class FileOperationException : Exception
{
    public FileOperationException(string message, Exception inner) 
        : base(message, inner) { }
}