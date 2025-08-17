using Autodesk.Revit.UI;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Create.ExportClasses
{
    internal class FileTemplateCreator
    {
        public static Result CreateFileTemplate(ref string chosenFileName, string destDir)
        {
            // Opens a dialog box allowing the user to choose the filename and location for saving the file.
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Title = "Save As",
                Filter = "Revit files (*.rvt)|*.rvt|All files (*.*)|*.*",
                DefaultExt = "rvt",
                AddExtension = true
            };

            bool? result = saveFileDialog.ShowDialog();
            if (result != true)
            {
                //message = "Operation cancelled by the user.";
                return Result.Cancelled;
            }

            chosenFileName = saveFileDialog.FileName;

            try
            {
                string buildFilesPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string sourceDir = Path.Combine(buildFilesPath, "build_files", "EmptyTemplate");
                CopyDirectory(sourceDir, destDir);
            }
            catch (Exception ex)
            {
                //message = $"Error copying template files: {ex.Message}";
                return Result.Failed;
            }

            return Result.Succeeded;
        }

        // Copies all the 'empty' template files to the folder that will form the final Ekahau file.
        private static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (string subdir in Directory.GetDirectories(sourceDir))
            {
                string destSubdir = Path.Combine(destDir, Path.GetFileName(subdir));
                CopyDirectory(subdir, destSubdir);
            }
        }
    }
}
