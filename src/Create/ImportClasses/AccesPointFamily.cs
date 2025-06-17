using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace Create.ImportClasses
{
    internal class AccesPointFamily
    {
        // This function loads the Access_Point.rfa file, which contains the 3D model of the access point, and integrates that model into the Revit project.
        public static FamilySymbol LoadAccessPointFamily(Document doc)
        {
            string assemblyPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string familyPath = Path.Combine(assemblyPath, "build_files", "build_tools", "Access_Point.rfa");

            if (!File.Exists(familyPath))
                return null;

            Family family;
            using (Transaction tx = new Transaction(doc, "Load Access Point Family"))
            {
                tx.Start();
                doc.LoadFamily(familyPath, out family);
                tx.Commit();
            }

            FamilySymbol symbol = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .FirstOrDefault(fs => fs.Family.Name.Equals("Access_Point", StringComparison.OrdinalIgnoreCase));

            if (symbol != null && !symbol.IsActive)
            {
                using (Transaction tx = new Transaction(doc, "Activate Access Point Symbol"))
                {
                    tx.Start();
                    symbol.Activate();
                    tx.Commit();
                }
            }

            return symbol;
        }

    }
}
