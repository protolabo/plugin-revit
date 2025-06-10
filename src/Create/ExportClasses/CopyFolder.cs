using Autodesk.Revit.UI;
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
    internal class CopyFolder
    {
        public static Result PrepareFolder(ref string chosenFileName, ref string message, string destDir)
        {
            // Opens a dialog box allowing the user to choose the filename and location for saving the file.
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Title = "Save As";
                saveFileDialog.Filter = "Revit files (*.rvt)|*.rvt|All files (*.*)|*.*";
                saveFileDialog.DefaultExt = "rvt";
                saveFileDialog.AddExtension = true;

                DialogResult result = saveFileDialog.ShowDialog();

                if (result != DialogResult.OK)
                {
                    message = "Operation cancelled by the user.";
                    return Result.Cancelled;
                }

                chosenFileName = saveFileDialog.FileName;
            }

            try
            {
                
                //string buildFilesPath = Path.Combine(desktopPath, "build_files");
                //string sourceDir = Path.Combine(buildFilesPath, "monTemplate");
                string buildFilesPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string sourceDir = Path.Combine(buildFilesPath, "build_files", "monTemplate");

                CopyDirectory(sourceDir, destDir);
            }
            catch (Exception ex)
            {
                message = $"Error copying template files: {ex.Message}";
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
