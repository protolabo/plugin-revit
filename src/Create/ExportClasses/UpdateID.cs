using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Create.ExportClasses
{
    internal class UpdateID
    {
        public static Result CompleteJsonReferences(string destDir, string baseName)
        {
            // Paths for JSON files
            string projectPath = Path.Combine(destDir, "project.json");
            string configPath = Path.Combine(destDir, "projectConfiguration.json");

            // Get id from projectConfiguration.json 
            string configJson = File.ReadAllText(configPath);
            string configId = Regex.Match(configJson, @"""id""\s*:\s*""([^""]+)""").Groups[1].Value;

            // Current UTC date ISO 8601 with Z
            string isoDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            // Update the fields in the project.json file:
            // - "name" and "title" are set to the baseName (usually the chosen filename without extension).
            // - "modifiedAt" and "createdAt" are updated to the current ISO date/time string.
            // - "projectConfigurationId" is updated to the provided configId.
            // Finally, the modified JSON content is written back to the project.json file.
            string projectJson = File.ReadAllText(projectPath);
            projectJson = Regex.Replace(projectJson, @"""name""\s*:\s*""[^""]*""", $"\"name\": \"{baseName}\"");
            projectJson = Regex.Replace(projectJson, @"""title""\s*:\s*""[^""]*""", $"\"title\": \"{baseName}\"");
            projectJson = Regex.Replace(projectJson, @"""modifiedAt""\s*:\s*""[^""]*""", $"\"modifiedAt\": \"{isoDate}\"");
            projectJson = Regex.Replace(projectJson, @"""createdAt""\s*:\s*""[^""]*""", $"\"createdAt\": \"{isoDate}\"");
            projectJson = Regex.Replace(projectJson, @"""projectConfigurationId""\s*:\s*""[^""]*""", $"\"projectConfigurationId\": \"{configId}\"");
            File.WriteAllText(projectPath, projectJson);

            // Update projectHistorys.json
            string historyPath = Path.Combine(destDir, "projectHistorys.json");
            string offsetDate = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");
            string projectJsonId = Regex.Match(projectJson, @"""id""\s*:\s*""([^""]+)""").Groups[1].Value;

            string historyJson = File.ReadAllText(historyPath);
            historyJson = Regex.Replace(historyJson, @"""timestamp""\s*:\s*""[^""]*""", $"\"timestamp\": \"{offsetDate}\"");
            historyJson = Regex.Replace(historyJson, @"""projectId""\s*:\s*""[^""]*""", $"\"projectId\": \"{projectJsonId}\"");
            historyJson = Regex.Replace(historyJson, @"""projectName""\s*:\s*""[^""]*""", $"\"projectName\": \"{baseName}\"");
            File.WriteAllText(historyPath, historyJson);

            // Alias mapping for usageProfiles.json
            //string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            //string FilesPath = Path.Combine(desktop, "build_files");
            //string FilesPathAlias = Path.Combine(FilesPath, "build_tools");
            string FilesPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string FilesPathAlias = Path.Combine(FilesPath, "build_files", "build_tools");

            // The keys/names of objects in usageProfiles.json and applicationProfiles.json do not match exactly.
            // For example, the object with "name": "Normal SLA (2 Mbps)" in usageProfiles corresponds to
            // "name": "SLA, Normal" in applicationProfiles, so a direct lookup is not possible.
            // To solve this, an aliases.json file was created containing entries like:
            // {
            //    "nombre": "Normal SLA (2 Mbps)",
            //    "alias": ["SLA, Normal"]
            // }
            // This file maps each main name to its possible aliases, enabling linking these objects correctly
            // by assigning the corresponding IDs based on these alias relationships.
            string usageProfilesPath = Path.Combine(destDir, "usageProfiles.json");
            string applicationProfilesPath = Path.Combine(destDir, "applicationProfiles.json");
            string aliasesPath = Path.Combine(FilesPathAlias, "aliases.json");

            var usageJson = JObject.Parse(File.ReadAllText(usageProfilesPath));
            var appJson = JObject.Parse(File.ReadAllText(applicationProfilesPath));
            var aliasJson = JObject.Parse(File.ReadAllText(aliasesPath));

            var appDict = appJson["applicationProfiles"]
                .ToDictionary(
                    a => a["name"]?.ToString() ?? "",
                    a => a["id"]?.ToString() ?? ""
                );

            var aliasDict = aliasJson["aliases"]
                .ToDictionary(
                    a => a["nombre"]?.ToString() ?? "",
                    a => a["alias"]?.Select(x => x.ToString()).ToList() ?? new List<string>()
                );

            foreach (var usage in usageJson["usageProfiles"])
            {
                string usageName = usage["name"]?.ToString() ?? "";
                List<string> aliasList = aliasDict.TryGetValue(usageName, out var foundAliases)
                    ? foundAliases
                    : new List<string>();

                var matchingIds = appDict
                    .Where(pair => aliasList.Contains(pair.Key))
                    .Select(pair => pair.Value)
                    .ToList();

                usage["applicationProfileIds"] = new JArray(matchingIds);
            }

            File.WriteAllText(usageProfilesPath, usageJson.ToString(Newtonsoft.Json.Formatting.Indented));

            return Result.Succeeded;
        }
    }
}
