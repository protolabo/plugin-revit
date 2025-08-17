using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Create.ImportClasses
{
    internal class JsonFilesExtractor
    {
        // This function displays a dialog box that allows the user to select an Ekahau file from which the access points will be loaded.
        // Then, it loads the accessPoints.json file from the Ekahau archive to retrieve the list of access points and their positions,
        // and the floorPlans.json file to obtain the floor view associated with those access points.
        public static bool GetJsonFromESX(out List<JObject> floorPlans, 
            out List<JObject> accessPoints,
            out List<JObject> antennaTypes,
            out List<JObject> simulatedRadios)
        {
            floorPlans = null;
            accessPoints = null;
            antennaTypes = null;
            simulatedRadios = null;

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Ekahau Project (*.esx)|*.esx",
                Title = "Select an .esx file"
            };

            bool? result = openFileDialog.ShowDialog(); 

            if (result != true)
                return false;

            string esxPath = openFileDialog.FileName;
            JObject floorPlansJson = null;
            JObject accessPointsJson = null;
            JObject antennaTypesJson = null;
            JObject simulatedRadiosJson = null;

            using (ZipArchive archive = ZipFile.OpenRead(esxPath))
            {
                foreach (var entry in archive.Entries)
                {
                    if (entry.FullName.EndsWith("floorPlans.json", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var reader = new StreamReader(entry.Open()))
                        {
                            floorPlansJson = JObject.Parse(reader.ReadToEnd());
                        }
                    }
                    else if (entry.FullName.EndsWith("accessPoints.json", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var reader = new StreamReader(entry.Open()))
                        {
                            accessPointsJson = JObject.Parse(reader.ReadToEnd());
                        }
                    }
                    else if (entry.FullName.EndsWith("antennaTypes.json", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var reader = new StreamReader(entry.Open()))
                        {
                            antennaTypesJson = JObject.Parse(reader.ReadToEnd());
                        }
                    }
                    else if (entry.FullName.EndsWith("simulatedRadios.json", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var reader = new StreamReader(entry.Open()))
                        {
                            simulatedRadiosJson = JObject.Parse(reader.ReadToEnd());
                        }
                    }
                }
            }

            if (floorPlansJson == null || accessPointsJson == null || antennaTypesJson == null || simulatedRadiosJson == null)
                return false;

            floorPlans = floorPlansJson["floorPlans"].ToObject<List<JObject>>();
            accessPoints = accessPointsJson["accessPoints"].ToObject<List<JObject>>();
            antennaTypes = antennaTypesJson["antennaTypes"].ToObject<List<JObject>>();
            simulatedRadios = simulatedRadiosJson["simulatedRadios"].ToObject<List<JObject>>();
            return true;
        }

    }
}
