using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Windows;
using Microsoft.Win32;


namespace Create.ImportClasses
{
    internal class EsxJsons
    {
        // This function displays a dialog box that allows the user to select an Ekahau file from which the access points will be loaded.
        // Then, it loads the accessPoints.json file from the Ekahau archive to retrieve the list of access points and their positions,
        // and the floorPlans.json file to obtain the floor view associated with those access points.
        public static bool LoadEsxJsons(out List<JObject> floorPlans, out List<JObject> accessPoints)
        {
            floorPlans = null;
            accessPoints = null;

            var openFileDialog = new OpenFileDialog
            {
                Filter = "Ekahau Project (*.esx)|*.esx",
                Title = "Select an .esx file"
            };

            if (openFileDialog.ShowDialog() != true)
                return false;

            string esxPath = openFileDialog.FileName;

            try
            {
                JObject floorPlansJson = null;
                JObject accessPointsJson = null;

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
                    }
                }

                if (floorPlansJson == null || accessPointsJson == null)
                {
                    MessageBox.Show("Required JSON files not found in the archive",
                                    "Error",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                    return false;
                }

                floorPlans = floorPlansJson["floorPlans"].ToObject<List<JObject>>();
                accessPoints = accessPointsJson["accessPoints"].ToObject<List<JObject>>();

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading ESX file: {ex.Message}",
                              "Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
                return false;
            } 
        }
    }
}
