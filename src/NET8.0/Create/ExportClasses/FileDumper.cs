using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using Newtonsoft.Json;

namespace Create.ExportClasses
{
    internal class FileDumper
    {
        public static void DumpViewInfo(object viewInfo, object modelData)
        {
            // Get desktop path
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            // --- Save viewInfo as JSON on desktop ---
            try
            {
                string viewInfoJson = JsonConvert.SerializeObject(viewInfo, Formatting.Indented);
                string outputPath = Path.Combine(desktopPath, "view_info.json");

                File.WriteAllText(outputPath, viewInfoJson);
                Console.WriteLine($"File view_info.json saved at: {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving view_info.json: {ex.Message}");
            }

            // --- Save modelData as JSON on desktop ---
            string outputFilePath = Path.Combine(desktopPath, "model_data.json");

            var options = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            };

            try
            {
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(modelData, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(outputFilePath, json);
                // TaskDialog.Show("Export", $"File saved:\n{outputFilePath}");
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Failed to save model_data.json:\n{ex.Message}");
            }
        }

    }
}
