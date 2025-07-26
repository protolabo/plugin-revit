using Create.ExportClasses;
using System.Reflection;

using Create.ExportClasses;

public static class GettersTester
{
    public static void RunTest()
    {

        // Verify that the value obtained from the Getters class corresponds to a valid GUID
        string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string buildFilesDir = Path.Combine(assemblyFolder, "build_files");
        string tempFolderPath = Path.Combine(buildFilesDir, "tempFolder");
        string tempPath = Path.Combine(tempFolderPath, "Template");

        var wallTypesJson = File.ReadAllText(Path.Combine(tempPath, "wallTypes.json"));
        string wallId = Getters.GetWallId("Exterior - Brick on Mtl. Stud", wallTypesJson);

        if (Guid.TryParse(wallId, out Guid parsedGuid))
        {
            Console.WriteLine($"✅ Wall ID is a valid GUID: {parsedGuid}");
        }
        else
        {
            Console.WriteLine($"❌ Wall ID is NOT a valid GUID: {wallId}");
        }
    }
}