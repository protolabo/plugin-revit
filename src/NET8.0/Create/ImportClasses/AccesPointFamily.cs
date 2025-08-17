using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace Create.ImportClasses
{
    internal class AccessPointFamily
    {
        // Generic method to load any family by file name and family name
        public static FamilySymbol LoadFamily(Document doc, string fileName, string familyName)
        {
            string assemblyPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string familyPath = Path.Combine(assemblyPath, "build_files", "build_tools", fileName);

            if (!File.Exists(familyPath))
                return null;

            Family family;
            using (Transaction tx = new Transaction(doc, $"Load {familyName} Family"))
            {
                tx.Start();
                doc.LoadFamily(familyPath, out family);
                tx.Commit();
            }

            FamilySymbol symbol = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .FirstOrDefault(fs => fs.Family.Name.Equals(familyName, StringComparison.OrdinalIgnoreCase));

            if (symbol != null && !symbol.IsActive)
            {
                using (Transaction tx = new Transaction(doc, $"Activate {familyName} Symbol"))
                {
                    tx.Start();
                    symbol.Activate();
                    tx.Commit();
                }
            }

            return symbol;
        }

        // Specific method to load Access Point family
        public static FamilySymbol LoadAccessPointFamily(Document doc)
        {
            return LoadFamily(doc, "Access_Point.rfa", "Access_Point");
        }

        // Specific method to load Bluetooth Beacon family
        public static FamilySymbol LoadBluetoothBeaconFamily(Document doc)
        {
            return LoadFamily(doc, "Bluetooth_Beacon.rfa", "Bluetooth_Beacon");
        }
    }
}

