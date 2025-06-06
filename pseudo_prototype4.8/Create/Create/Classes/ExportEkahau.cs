using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Create.Helpers
{
    internal class ExportEkahau
    {
        public static Result CreateEsx(string chosenFileName)
        {
            // Ruta a la carpeta de trabajo
            string myCopyPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "myCopy");

            // Ruta de destino del archivo .esx
            string outputZipPath = Path.ChangeExtension(chosenFileName, ".esx");

            // Eliminar si ya existe
            if (File.Exists(outputZipPath))
            {
                File.Delete(outputZipPath);
            }

            // Crear el archivo zip (con extensión .esx)
            ZipFile.CreateFromDirectory(myCopyPath, outputZipPath, CompressionLevel.Optimal, false);

            // Confirmación
            TaskDialog.Show("Success", $"File exported to:\n{outputZipPath}");

            return Result.Succeeded;
        }
    }
}
