using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Create.Helpers
{
    internal class CreateID
    {
        public static Result GenerateIDInFiles(string destDir)
        {
            string[] jsonFiles = Directory.GetFiles(destDir, "*.json", SearchOption.AllDirectories);
            foreach (var jsonFile in jsonFiles)
            {
                string content = File.ReadAllText(jsonFile);

                // Replace "id": "" by "id": "GUID" in all files
                string updated = Regex.Replace(
                    content,
                    @"""id""\s*:\s*""""",
                    m => $"\"id\": \"{Guid.NewGuid()}\""
                );

                File.WriteAllText(jsonFile, updated);
            }

            return Result.Succeeded;
        }
    }
}
