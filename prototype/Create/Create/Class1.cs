using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;

using System.Drawing;
using System.IO.Compression;

using System.IO.Compression;

namespace Create
{
    [Transaction(TransactionMode.ReadOnly)]
    public class Class1 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc2 = commandData.Application.ActiveUIDocument;
            Document doc2 = uidoc2.Document;

            // Show WPF window
            SelectionWindow window = new SelectionWindow();
            bool? result2 = window.ShowDialog();

            if (result2 != true || window.SelectedCategories.Count == 0)
            {
                TaskDialog.Show("Notice", "No category was selected.");
                return Result.Cancelled;
            }

            string rutaArchivo = Path.Combine(
                 Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                 "build_files", "build_tools", "elements.txt"
             );


            var elementos = new List<Element>();

            foreach (int categoriaInt in window.SelectedCategories)
            {
                BuiltInCategory categoria = (BuiltInCategory)categoriaInt;

                var collector = new FilteredElementCollector(doc2)
                                    .OfCategory(categoria)
                                    .WhereElementIsNotElementType()
                                    .ToElements();

                elementos.AddRange(collector);
            }

            using (StreamWriter sw = new StreamWriter(rutaArchivo))
            {
                foreach (var elemento in elementos)
                {
                    string tipo = elemento.Category?.Name ?? "Unknown";
                    string info = $"{tipo} | ID: {elemento.Id.IntegerValue} - {elemento.Name}";

                    // Wall: line (start - end)
                    if (elemento is Wall wall && wall.Location is LocationCurve lc)
                    {
                        XYZ start = lc.Curve.GetEndPoint(0);
                        XYZ end = lc.Curve.GetEndPoint(1);
                        info += $" | Start: ({start.X:F2}, {start.Y:F2}, {start.Z:F2})";
                        info += $" | End: ({end.X:F2}, {end.Y:F2}, {end.Z:F2})";
                    }
                    // Door or Window: point
                    else if (elemento.Location is LocationPoint lp)
                    {
                        XYZ pt = lp.Point;
                        info += $" | Position: ({pt.X:F2}, {pt.Y:F2}, {pt.Z:F2})";
                    }

                    sw.WriteLine(info);
                }
            }

            string chosenFileName = null;

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                // This block only selects the file name; the extension is added later
                // !!! This block must contain these values, even if they are not used, or the plugin will not work
                saveFileDialog.Title = "Save As";
                saveFileDialog.Filter = "Revit files (*.rvt)|*.rvt|All files (*.*)|*.*";
                saveFileDialog.DefaultExt = "rvt";
                saveFileDialog.AddExtension = true;

                DialogResult result = saveFileDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    chosenFileName = saveFileDialog.FileName;
                }
                else
                {
                    message = "Operation cancelled by the user.";
                    return Result.Cancelled;
                }
            }
            string baseName = Path.GetFileNameWithoutExtension(chosenFileName);

            try
            {
                // Copies the EMPTY files from Desktop/build_files/monTemplate folder to the folder that will contain the JSON files for the Ekahau file.
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string buildFilesPath = Path.Combine(desktopPath, "build_files");
                string sourceDir = Path.Combine(buildFilesPath, "monTemplate");
                string destDir = Path.Combine(desktopPath, "myCopy");

                // Copy folder
                CopyDirectory(sourceDir, destDir);

                // Generates a GUID for the 'id' field of all objects within the JSON files
                string[] jsonFiles = Directory.GetFiles(destDir, "*.json", SearchOption.AllDirectories);
                foreach (var jsonFile in jsonFiles)
                {
                    string content = File.ReadAllText(jsonFile);

                    // Replace "id": "" by "id": "GUID"
                    string updated = Regex.Replace(
                        content,
                        @"""id""\s*:\s*""""",
                        m => $"\"id\": \"{Guid.NewGuid()}\""
                    );

                    File.WriteAllText(jsonFile, updated);
                }

                // Some JSON files reference object IDs contained in other JSON files.
                // This block completes those references

                // JSON path
                string projectPath = Path.Combine(destDir, "project.json");
                string configPath = Path.Combine(destDir, "projectConfiguration.json");

                // Get id from projectConfiguration.json 
                string configJson = File.ReadAllText(configPath);
                string configId = Regex.Match(configJson, @"""id""\s*:\s*""([^""]+)""").Groups[1].Value;

                // Get the current date in UTC ISO 8601 format with 'Z'.
                string isoDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

                // Fills the empty fields in project.json with information provided by the user as well as data obtained from other files.
                string projectJson = File.ReadAllText(projectPath);
                projectJson = Regex.Replace(projectJson, @"""name""\s*:\s*""[^""]*""", $"\"name\": \"{baseName}\"");
                projectJson = Regex.Replace(projectJson, @"""title""\s*:\s*""[^""]*""", $"\"title\": \"{baseName}\"");
                projectJson = Regex.Replace(projectJson, @"""modifiedAt""\s*:\s*""[^""]*""", $"\"modifiedAt\": \"{isoDate}\"");
                projectJson = Regex.Replace(projectJson, @"""createdAt""\s*:\s*""[^""]*""", $"\"createdAt\": \"{isoDate}\"");
                projectJson = Regex.Replace(projectJson, @"""projectConfigurationId""\s*:\s*""[^""]*""", $"\"projectConfigurationId\": \"{configId}\"");

                // Save changes
                File.WriteAllText(projectPath, projectJson);

                // projectHistory.json path
                string historyPath = Path.Combine(destDir, "projectHistorys.json");

                // Get the local time zone with offset (e.g., -04:00).
                string offsetDate = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");

                // Get projectId from project.json 
                string projectJsonId = Regex.Match(projectJson, @"""id""\s*:\s*""([^""]+)""").Groups[1].Value;

                // Fills the empty fields in projectHistory.json.
                string historyJson = File.ReadAllText(historyPath);
                historyJson = Regex.Replace(historyJson, @"""timestamp""\s*:\s*""[^""]*""", $"\"timestamp\": \"{offsetDate}\"");
                historyJson = Regex.Replace(historyJson, @"""projectId""\s*:\s*""[^""]*""", $"\"projectId\": \"{projectJsonId}\"");
                historyJson = Regex.Replace(historyJson, @"""projectName""\s*:\s*""[^""]*""", $"\"projectName\": \"{baseName}\"");

                File.WriteAllText(historyPath, historyJson);

                // The usageProfiles.json file references the Applications contained in applicationProfiles.json,
                // but the names of these objects do not match, making it impossible to obtain the IDs directly.
                // To solve this, I created a dictionary in Desktop/build_files/build_tools/aliases.json that maps
                // the application names in usageProfiles.json to the correct application names in applicationProfiles.json,
                // allowing retrieval of the correct Application ID.
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string FilesPath = Path.Combine(desktop, "build_files");
                string FilesPathAlias = Path.Combine(FilesPath, "build_tools");
                string usageProfilesPath = Path.Combine(destDir, "usageProfiles.json");
                string applicationProfilesPath = Path.Combine(destDir, "applicationProfiles.json");
                string aliasesPath = Path.Combine(FilesPathAlias, "aliases.json");

                // Read and parse the JSON files.
                var usageJson = JObject.Parse(File.ReadAllText(usageProfilesPath));
                var appJson = JObject.Parse(File.ReadAllText(applicationProfilesPath));
                var aliasJson = JObject.Parse(File.ReadAllText(aliasesPath));

                // Create dictionary: name → applicationProfiles id.
                var appDict = appJson["applicationProfiles"]
                    .ToDictionary(
                        a => a["name"]?.ToString() ?? "",
                        a => a["id"]?.ToString() ?? ""
                    );

                // Create dictionary: usageName → list of aliases.
                var aliasDict = aliasJson["aliases"]
                    .ToDictionary(
                        a => a["nombre"]?.ToString() ?? "",
                        a => a["alias"]?.Select(x => x.ToString()).ToList() ?? new List<string>()
                    );

                // Update usageProfiles.json
                foreach (var usage in usageJson["usageProfiles"])
                {
                    string usageName = usage["name"]?.ToString() ?? "";
                    List<string> aliasList = aliasDict.TryGetValue(usageName, out var foundAliases)
                        ? foundAliases
                        : new List<string>();

                    // Find applicationProfiles IDs that match any alias.
                    var matchingIds = appDict
                        .Where(pair => aliasList.Contains(pair.Key))
                        .Select(pair => pair.Value)
                        .ToList();

                    usage["applicationProfileIds"] = new JArray(matchingIds);
                }

                // Save changes
                File.WriteAllText(usageProfilesPath, usageJson.ToString(Newtonsoft.Json.Formatting.Indented));

                // To use an image as a 'map' (background) in Ekahau, the image must have a specific name
                // and require the creation of two additional files: floorPlans.json and images.json.
                // An empty version of these two files is available in Desktop/build_files/monTemplate, ready to be used.

                // Export the Revit model as a BMP image
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                Document doc = uidoc.Document;
                Autodesk.Revit.DB.View view = uidoc.ActiveView;

                // Path to output file 
                string desktopBMP = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string bmpPath = Path.Combine(desktopBMP, @"build_files\build_tools\exported_view.bmp");

                ImageExportOptions imageOptions = new ImageExportOptions();
                imageOptions.ExportRange = ExportRange.SetOfViews;
                imageOptions.HLRandWFViewsFileType = ImageFileType.BMP;
                imageOptions.FilePath = bmpPath;
                imageOptions.ZoomType = ZoomFitType.FitToPage;
                imageOptions.PixelSize = 1500;
                imageOptions.SetViewsAndSheets(new List<ElementId>() { view.Id });

                doc.ExportImage(imageOptions);

                // In case the model contains multiple plans, we only use the first exported image (for testing purposes only).
                // Get BMP image
                string exportDir = Path.Combine(desktopBMP, "build_files", "build_tools");
                string[] exportedBmps = Directory.GetFiles(exportDir, "exported_view - *.bmp");

                if (exportedBmps.Length > 0)
                {
                    string sourceImagePath = exportedBmps[0]; // Use only the first one
                    string originalImageName = Path.GetFileName(sourceImagePath);

                    // The image must have the specific name: image-GUID
                    string imageId = Guid.NewGuid().ToString();
                    string imageName = $"image-{imageId}";

                    string destImagePath = Path.Combine(destDir, imageName);

                    // Copy the image with the modified name and place it along with the rest of the JSON files
                    // that will make up the final ESX (Ekahau) file
                    File.Copy(sourceImagePath, destImagePath, true);

                    // Get image properties
                    using (var img = System.Drawing.Image.FromFile(destImagePath))
                    {
                        double width = img.Width;
                        double height = img.Height;

                        // ----------------- Update images.json -----------------
                        string imagesJsonPath = Path.Combine(destDir, "images.json");
                        if (File.Exists(imagesJsonPath))
                        {
                            string jsonText = File.ReadAllText(imagesJsonPath);
                            JObject imagesObj = JObject.Parse(jsonText);

                            var imageEntry = imagesObj["images"]?[0];
                            if (imageEntry != null)
                            {
                                imageEntry["imageFormat"] = "BMP";
                                imageEntry["resolutionWidth"] = width;
                                imageEntry["resolutionHeight"] = height;
                                imageEntry["id"] = imageId;
                            }

                            File.WriteAllText(imagesJsonPath, imagesObj.ToString(Formatting.Indented));
                        }

                        // ----------------- Update floorPlans.json -----------------
                        string floorPlansPath = Path.Combine(destDir, "floorPlans.json");
                        if (File.Exists(floorPlansPath))
                        {
                            string floorJson = File.ReadAllText(floorPlansPath);
                            JObject floorObj = JObject.Parse(floorJson);

                            var floorEntry = floorObj["floorPlans"]?[0];
                            if (floorEntry != null)
                            {
                                floorEntry["name"] = originalImageName; // The original name of the exported image contains relevant information about the model.
                                floorEntry["width"] = width;
                                floorEntry["height"] = height;
                                floorEntry["imageId"] = imageId;
                                floorEntry["cropMaxX"] = width;
                                floorEntry["cropMaxY"] = height;
                                floorEntry["id"] = Guid.NewGuid().ToString();
                            }

                            File.WriteAllText(floorPlansPath, floorObj.ToString(Formatting.Indented));
                        }
                    }
                }
                else
                {
                    TaskDialog.Show("Error", "The exported BMP image was not found.");
                }

                // Create WallPoints and WallSegments files for Ekahau zip file
                // Carpeta destino
                string myCopyFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "myCopy");

                if (!Directory.Exists(myCopyFolder))
                {
                    TaskDialog.Show("Error", "myCopy folder not found.");
                    return Result.Failed;
                }

                // Read floorPlanId
                // Test Code !!! JUST ONE FLOOR FOR NOW (Groups[1].Value)
                string floorPlansPath2 = Path.Combine(myCopyFolder, "floorPlans.json");
                string floorPlansJson = File.ReadAllText(floorPlansPath2);
                string floorPlanId = Regex.Match(floorPlansJson, @"""id""\s*:\s*""([^""]+)""").Groups[1].Value;

                // Load wallTypeId
                // Test Code!!! ONLY CONCRETE WALL FOR NOW (@"""name""\s*:\s*""Wall, Concrete"")
                string wallTypesPath = Path.Combine(myCopyFolder, "wallTypes.json");
                string wallTypesJson = File.ReadAllText(wallTypesPath);
                var matchWallTypeId = Regex.Match(wallTypesJson, @"""name""\s*:\s*""Wall, Concrete"".*?""id""\s*:\s*""([^""]+)""", RegexOptions.Singleline);
                //if (!matchWallTypeId.Success)
                //{
                //    TaskDialog.Show("Error", "No WallType with name 'Wall, Concrete' in wallTypes.json");
                //    return Result.Failed;
                //}
                string wallTypeId = matchWallTypeId.Groups[1].Value;

                // Get All walls from file
                var walls = new FilteredElementCollector(doc)
                                .OfClass(typeof(Wall))
                                .WhereElementIsNotElementType()
                                .Cast<Wall>()
                                .Where(w => w.Location is LocationCurve)
                                .ToList();

                if (walls.Count == 0)
                {
                    TaskDialog.Show("Notice", "No walls were found.");
                    return Result.Failed;
                }

                // Create Lists
                var wallPointsList = new List<string>();
                var wallSegmentsList = new List<string>();
                var pointIds = new List<string>();

                foreach (var wall in walls)
                {
                    var locCurve = (LocationCurve)wall.Location;
                    XYZ start = locCurve.Curve.GetEndPoint(0);
                    XYZ end = locCurve.Curve.GetEndPoint(1);

                    string idStart = Guid.NewGuid().ToString();
                    string idEnd = Guid.NewGuid().ToString();

                    pointIds.Add(idStart);
                    pointIds.Add(idEnd);

                    wallPointsList.Add($@"{{
                      ""location"": {{
                        ""floorPlanId"": ""{floorPlanId}"",
                        ""coord"": {{
                          ""x"": {start.X},
                          ""y"": {start.Y}
                        }}
                      }},
                      ""id"": ""{idStart}"",
                      ""status"": ""CREATED""
                    }}");

                                    wallPointsList.Add($@"{{
                      ""location"": {{
                        ""floorPlanId"": ""{floorPlanId}"",
                        ""coord"": {{
                          ""x"": {end.X},
                          ""y"": {end.Y}
                        }}
                      }},
                      ""id"": ""{idEnd}"",
                      ""status"": ""CREATED""
                    }}");

                                    wallSegmentsList.Add($@"{{
                      ""wallPoints"": [
                        ""{idStart}"",
                        ""{idEnd}""
                      ],
                      ""wallTypeId"": ""{wallTypeId}"",
                      ""originType"": ""WALL_TOOL"",
                      ""id"": ""{Guid.NewGuid()}"",
                      ""status"": ""CREATED""
                    }}");
                }

                // Save wallPoints.json
                string wallPointsPath = Path.Combine(myCopyFolder, "wallPoints.json");
                File.WriteAllText(wallPointsPath, "{\n  \"wallPoints\": [\n" + string.Join(",\n", wallPointsList) + "\n  ]\n}");

                // Save wallSegments.json
                string wallSegmentsPath = Path.Combine(myCopyFolder, "wallSegments.json");
                File.WriteAllText(wallSegmentsPath, "{\n  \"wallSegments\": [\n" + string.Join(",\n", wallSegmentsList) + "\n  ]\n}");

                // CREATE ZIP FILE AND CHANGE FILKE EXTENSION
                // At this point, all the JSON files and the image are ready to create the final ESX(Ekahau) file.
                // The only step left is to create a compressed(zip) file with all the files and change the extension from .zip to .esx.

                // Path to folder 
                string myCopyPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "myCopy");

                // Destination path of the .esx file (it is just a .zip with a different extension)
                string outputZipPath = Path.ChangeExtension(chosenFileName, ".esx");

                // If it already exists, delete it.
                if (File.Exists(outputZipPath))
                {
                    File.Delete(outputZipPath);
                }

                // Create zip file
                ZipFile.CreateFromDirectory(myCopyPath, outputZipPath, CompressionLevel.Optimal, false);

                TaskDialog.Show("Success", $"File exported to:\n{outputZipPath}");

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private void CopyDirectory(string sourceDir, string destDir)
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