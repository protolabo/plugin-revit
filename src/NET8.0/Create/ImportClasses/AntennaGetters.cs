using System;
using System.Collections.Generic;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;

namespace Create.ExportClasses
{
    internal class AntennaGetters
    {
        /// <summary>
        /// Retrieves the IDs for "Generic 2.2dBi Omni" and "Generic 2.2dBi Omni BLE"
        /// from a list of antenna type JSON objects.
        /// </summary>
        /// <param name="antennaTypes">List of antenna type JObject entries.</param>
        /// <param name="idOmni">Output: ID for "Generic 2.2dBi Omni".</param>
        /// <param name="idOmniBle">Output: ID for "Generic 2.2dBi Omni BLE".</param>
        public static void GetAntennaIds(List<JObject> antennaTypes, out string idOmni, out string idOmniBle)
        {
            idOmni = string.Empty;
            idOmniBle = string.Empty;

            // Fixed antenna names
            string antennaNameOmni = "Generic 2.2dBi Omni";
            string antennaNameOmniBle = "Generic 2.2dBi Omni BLE";

            try
            {
                if (antennaTypes == null || antennaTypes.Count == 0)
                {
                    TaskDialog.Show("Error", "The list 'antennaTypes' is empty or null.");
                    return;
                }

                foreach (var antenna in antennaTypes)
                {
                    string name = (string)antenna["name"];
                    string id = (string)antenna["id"];

                    if (name == antennaNameOmni)
                        idOmni = id;

                    if (name == antennaNameOmniBle)
                        idOmniBle = id;
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Error in GetAntennaIds: {ex.Message}");
            }
        }
    }
}
