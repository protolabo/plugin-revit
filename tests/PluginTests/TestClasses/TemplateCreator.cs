using Newtonsoft.Json;
using Create.ExportClasses;
using System.Reflection;

public static class TemplateCreator
{
    public static void CreateTemplate(string tempPath)
    {
        string emptyTemplatePath = @"C:\Users\pelon\source\repos\Create\Create\bin\Debug\build_files\EmptyTemplate";

        // Delete contents of Template folder
        if (Directory.Exists(tempPath))
        {
            foreach (string file in Directory.GetFiles(tempPath))
            {
                File.Delete(file);
            }

            foreach (string dir in Directory.GetDirectories(tempPath))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
        else
        {
            Directory.CreateDirectory(tempPath);
        }

        // Copy files from EmptyTemplate to Template
        foreach (string sourceFilePath in Directory.GetFiles(emptyTemplatePath, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(emptyTemplatePath, sourceFilePath);
            string destinationPath = Path.Combine(tempPath, relativePath);

            string destinationDir = Path.GetDirectoryName(destinationPath);
            if (!Directory.Exists(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
            }

            File.Copy(sourceFilePath, destinationPath, overwrite: true);
        }

    }
}