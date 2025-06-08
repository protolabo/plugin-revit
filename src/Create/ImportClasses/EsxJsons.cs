using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Windows.Forms;

namespace Create.ImportClasses
{
    internal class EsxJsons
    {
        public static bool LoadEsxJsons(out List<JObject> floorPlans, out List<JObject> accessPoints)
        {
            floorPlans = null;
            accessPoints = null;

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Ekahau Project (*.esx)|*.esx",
                Title = "Select an .esx file"
            };

            if (openFileDialog.ShowDialog() != DialogResult.OK)
                return false;

            string esxPath = openFileDialog.FileName;
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
                return false;

            floorPlans = floorPlansJson["floorPlans"].ToObject<List<JObject>>();
            accessPoints = accessPointsJson["accessPoints"].ToObject<List<JObject>>();
            return true;
        }

    }
}
