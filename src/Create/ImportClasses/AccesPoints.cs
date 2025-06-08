using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;

namespace Create.ImportClasses
{
    internal class AccesPoints
    {
        public static int PlaceAccessPoints(
            Document doc,
            List<JObject> floorPlans,
            List<JObject> accessPoints,
            FamilySymbol symbol)
        {
            int placedCount = 0;

            var groupedByView = accessPoints
                .Where(ap => ap["location"]?["floorPlanId"] != null)
                .GroupBy(ap =>
                {
                    string floorPlanId = ap["location"]["floorPlanId"].ToString();
                    string viewName = floorPlans
                        .FirstOrDefault(f => f["id"]?.ToString() == floorPlanId)?["name"]
                        ?.ToString();

                    return string.IsNullOrEmpty(viewName) ? null : SubClasses.GetViewName.ExtractViewName(viewName);
                })
                .Where(g => g.Key != null);

            foreach (var group in groupedByView)
            {
                string extractedViewName = group.Key;

                ViewPlan view = new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewPlan))
                    .Cast<ViewPlan>()
                    .FirstOrDefault(v => v.Name == extractedViewName);

                if (view == null) continue;

                Level level = view.GenLevel != null ? doc.GetElement(view.GenLevel.Id) as Level : null;
                if (level == null) continue;

                BoundingBoxXYZ cropBox = view.CropBox;
                if (cropBox == null) continue;

                double minX = cropBox.Min.X;
                double maxX = cropBox.Max.X;
                double minY = cropBox.Min.Y;
                double maxY = cropBox.Max.Y;

                var floorPlan = floorPlans.FirstOrDefault(f => f["name"]?.ToString().Contains(extractedViewName) == true);
                if (floorPlan == null) continue;

                double imageWidth = floorPlan["width"].Value<double>();
                double imageHeight = floorPlan["height"].Value<double>();

                using (Transaction tx = new Transaction(doc, $"Place Access Points in {extractedViewName}"))
                {
                    tx.Start();

                    foreach (var ap in group)
                    {
                        double x_ekahau = ap["location"]["coord"]["x"].Value<double>();
                        double y_ekahau = ap["location"]["coord"]["y"].Value<double>();

                        double x_revit = (x_ekahau / imageWidth) * (maxX - minX) + minX;
                        double y_revit = maxY - (y_ekahau / imageHeight) * (maxY - minY);

                        XYZ location = new XYZ(x_revit, y_revit, 0);

                        doc.Create.NewFamilyInstance(location, symbol, level, StructuralType.NonStructural);

                        placedCount++;
                    }

                    tx.Commit();
                }
            }

            return placedCount;
        }

    }
}
