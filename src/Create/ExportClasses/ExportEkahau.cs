using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Create.ExportClasses
{
    internal class ExportEkahau
    {
        public static Result CreateEsx(string chosenFileName)
        {
            // Path to the working folder
            string myCopyPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "myCopy");

            // Destination path of the .esx file
            string outputZipPath = Path.ChangeExtension(chosenFileName, ".esx");

            // Replace if exists
            if (File.Exists(outputZipPath))
            {
                File.Delete(outputZipPath);
            }

            // Create the zip file (with .esx extension)
            ZipFile.CreateFromDirectory(myCopyPath, outputZipPath, CompressionLevel.Optimal, false);

            TaskDialog.Show("Success", $"File exported to:\n{outputZipPath}");

            return Result.Succeeded;
        }
    }
}
