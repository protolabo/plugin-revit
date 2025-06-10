using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Create.ExportClasses
{
    internal class Getters
    {
        public static string GetWallId(string wallTypesJson)
        {
            // For the moment all walls are 'Wall, Concrete', will be update later.
            return Regex.Match(wallTypesJson, @"""name""\s*:\s*""Wall, Concrete"".*?""id""\s*:\s*""([^""]+)""", RegexOptions.Singleline).Groups[1].Value;
        }

        public static string GetWindowId(string wallTypesJson)
        {
            return Regex.Match(wallTypesJson, @"""name""\s*:\s*""Window, Interior"".*?""id""\s*:\s*""([^""]+)""", RegexOptions.Singleline).Groups[1].Value;
        }

        public static string GetDoorId(string wallTypesJson)
        {
            return Regex.Match(wallTypesJson, @"""name""\s*:\s*""Door, Interior Office"".*?""id""\s*:\s*""([^""]+)""", RegexOptions.Singleline).Groups[1].Value;
        }

        public static string GetAreaId(string requirementsJson)
        {
            return Regex.Match(requirementsJson, @"""name""\s*:\s*""Ekahau Best Practices"".*?""id""\s*:\s*""([^""]+)""", RegexOptions.Singleline).Groups[1].Value;
        }
    }
}
