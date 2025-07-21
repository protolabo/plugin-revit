using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Create.ExportClasses
{
    internal class TemporaryFilesCollector
    {
        // This fonction deletes the temporary folder that contains the neccesary files to create
        // the final Ekahau file.
        public static void DeleteTemporaryFiles()
        {
            try
            {
                string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string tempFolderPath = Path.Combine(assemblyFolder, "build_files", "tempFolder");

                if (Directory.Exists(tempFolderPath))
                {
                    Directory.Delete(tempFolderPath, true); 
                    Console.WriteLine("Temporary folder deleted successfully.");
                }
                else
                {
                    Console.WriteLine("Temporary folder does not exist.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deleting temporary folder: " + ex.Message);
            }
        }
    }
}
