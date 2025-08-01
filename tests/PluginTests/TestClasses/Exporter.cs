using Newtonsoft.Json;
using Create.ExportClasses;
using System.Reflection;

using System.Drawing; 

public static class Exporter
{
    public static void SimulateExport(string model)
    {
        string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string buildFilesDir = Path.Combine(assemblyFolder, "build_files");
        string tempFolderPath = Path.Combine(buildFilesDir, "tempFolder");
        string tempPath = Path.Combine(tempFolderPath, "Template");

        // Delete all .bmp files in tempFolderPath
        foreach (string file in Directory.GetFiles(tempFolderPath, "*.bmp"))
        {
            File.Delete(file);
        }

        // // Delete all files starting with "image-" in tempFolderPath
        // foreach (string file in Directory.GetFiles(tempPath))
        // {
        //     if (Path.GetFileName(file).StartsWith("image-"))
        //     {
        //         File.Delete(file);
        //     }
        // }

        // Delete imageData.json if it exists
        string imageDataPath = Path.Combine(tempFolderPath, "imageData.json");
        if (File.Exists(imageDataPath))
        {
            File.Delete(imageDataPath);
        }

        bool allValid = true;

        // Read Model json configuration file
        string testFilePath = $"./TestFiles/Integration/Models/{model}";
        string jsonContent = File.ReadAllText(testFilePath);
        ModelInfo testInfo = JsonConvert.DeserializeObject<ModelInfo>(jsonContent);

        // Path to base image
        string sourceImagePath = Path.Combine(buildFilesDir, "build_tools", "template-image.bmp");
        var imageDataList = new List<ImageData>();

        foreach (var region in testInfo.cropRegions)
        {
            string levelNameFormatted = region.viewName.Replace("_", " ");
            string fileName = $"exported_view - Floor Plan - {levelNameFormatted}.bmp";
            string fullPath = Path.Combine(tempFolderPath, fileName);

            using (var originalImage = Image.FromFile(sourceImagePath))
            using (var resizedImage = new Bitmap(originalImage, region.width, region.height))
            {
                resizedImage.Save(fullPath); 
                // Console.WriteLine($"✅ Copied and resized to: {fullPath}");
            }

            imageDataList.Add(new ImageData
            {
                viewName = levelNameFormatted,
                min = region.min,
                max = region.max,
                width = region.width,
                height = region.height
            });
        }

        // Save imageData.json
        string imageDataJson = JsonConvert.SerializeObject(imageDataList, Formatting.Indented);
        // string imageDataPath = Path.Combine(tempFolderPath, "imageData.json");
        File.WriteAllText(imageDataPath, imageDataJson);
        Console.WriteLine("✅ All files generated.");

    }

}
