using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Create.ExportClasses
{
    internal class EkahauFileCreator
    {
        public static Result CreateEsxFile(string chosenFileName)
        {
            // Path to the working folder
            string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string buildFilesDir = Path.Combine(assemblyFolder, "build_files");
            string tempFolderPath = Path.Combine(buildFilesDir, "tempFolder");
            string tempPath = Path.Combine(tempFolderPath, "Template");


            // Destination path of the .esx file
            string outputZipPath = Path.ChangeExtension(chosenFileName, ".esx");

            // Replace if exists
            if (File.Exists(outputZipPath))
            {
                File.Delete(outputZipPath);
            }

            // Create the zip file (with .esx extension)
            ZipFile.CreateFromDirectory(tempPath, outputZipPath, CompressionLevel.Optimal, false);

            TaskDialog.Show("Success", $"File exported to:\n{outputZipPath}");

            return Result.Succeeded;
        }
    }
}
