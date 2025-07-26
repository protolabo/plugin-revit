using Create.ExportClasses;
using System.Reflection;

using Newtonsoft.Json.Linq;

using Create.ExportClasses;

public static class AttenuationUpdaterTester
{
    public static void RunTest()
    {
        string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string buildFilesDir = Path.Combine(assemblyFolder, "build_files");
        string tempFolderPath = Path.Combine(buildFilesDir, "tempFolder");
        string tempPath = Path.Combine(tempFolderPath, "Template");

        // Verify that the attenuation update is correctly performed from the file provided by 
        // the user to the corresponding Ekahau JSON file

        AttenuationUpdater.UpdateEkahauValues();

        string wallTypesFilePath = Path.Combine(tempPath, "wallTypes.json");

        JArray mappingArray = JArray.Parse(File.ReadAllText(Path.Combine(assemblyFolder, "wall_data.json")));
        JObject wallTypesJsonFile = JObject.Parse(File.ReadAllText(wallTypesFilePath));

        JArray wallTypes = (JArray)wallTypesJsonFile["wallTypes"];

        bool allMatched = true;
        int totalChecked = 0;

        foreach (var group in mappingArray)
        {
            foreach (var prop in group.Children<JProperty>())
            {
                var array = prop.Value as JArray;
                if (array == null) continue;

                foreach (var item in array.Children<JObject>())
                {
                    string ekahauName = item["Ekahau"]?.ToString();
                    double actualAttenuation = item["Attenuation"]?.ToObject<double>() ?? -1;

                    if (string.IsNullOrWhiteSpace(ekahauName))
                        continue;

                    var wallType = wallTypes.FirstOrDefault(wt => wt["name"]?.ToString() == ekahauName);
                    if (wallType == null)
                    {
                        Console.WriteLine($"⚠️ No wallType found for Ekahau name: '{ekahauName}'");
                        continue;
                    }

                    double thickness = wallType["thickness"]?.ToObject<double>() ?? 0;
                    var firstPropagation = wallType["propagationProperties"]?.FirstOrDefault();
                    double attenuationFactor = firstPropagation?["attenuationFactor"]?.ToObject<double>() ?? 0;

                    double expected = thickness * attenuationFactor;

                    totalChecked++;

                    double diff = Math.Abs(expected - actualAttenuation);
                    if (diff <= 0.1)
                    {
                        // Console.WriteLine($"✅ Match for '{ekahauName}': expected ~{expected:F4}, got {actualAttenuation}");
                    }
                    else
                    {
                        Console.WriteLine($"❌ Mismatch for '{ekahauName}': expected ~{expected:F4}, got {actualAttenuation}, diff = {diff:F6}");
                        allMatched = false;
                    }
                }
            }
        }

        if (totalChecked == 0)
        {
            Console.WriteLine("⚠️ No valid entries found to check attenuation.");
        }
        else if (allMatched)
        {
            Console.WriteLine($"✅ All {totalChecked} attenuation values match expected values.");
        }
        else
        {
            Console.WriteLine("❌ Some attenuation values do not match expected values.");
        }
    }
}
