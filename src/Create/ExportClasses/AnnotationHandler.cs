using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using System.Windows.Controls;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;

namespace Create.ExportClasses
{
    internal class AnnotationHandler
    {
        public static void HideAnnotations(Document doc, View view)
        {
            using (Transaction t = new Transaction(doc, "Set Crop to Walls and Annotations"))
            {
                t.Start();

                // Activate crop if it is not active
                if (!view.CropBoxActive)
                    view.CropBoxActive = true;

                if (!view.CropBoxVisible)
                    view.CropBoxVisible = true;

                // Initialize min and max
                XYZ globalMin = new XYZ(double.MaxValue, double.MaxValue, double.MaxValue);
                XYZ globalMax = new XYZ(double.MinValue, double.MinValue, double.MinValue);

                // Categories to include: walls + annotations
                var categories = new List<BuiltInCategory>
                    {
                        BuiltInCategory.OST_Walls,
                        BuiltInCategory.OST_TextNotes,
                        BuiltInCategory.OST_Dimensions,
                        BuiltInCategory.OST_Levels,
                        BuiltInCategory.OST_Grids
                    };

                // Create a list of filters compatible with LogicalOrFilter
                IList<ElementFilter> filters = categories
                    .Select(cat => (ElementFilter)new ElementCategoryFilter(cat))
                    .ToList();

                LogicalOrFilter orFilter = new LogicalOrFilter(filters);

                // Get elements in the view
                var elements = new FilteredElementCollector(doc, view.Id)
                                    .WherePasses(orFilter)
                                    .WhereElementIsNotElementType()
                                    .ToElements();

                foreach (var el in elements)
                {
                    BoundingBoxXYZ bbox = el.get_BoundingBox(view);
                    if (bbox == null) continue;

                    // Compare min
                    globalMin = new XYZ(
                        Math.Min(globalMin.X, bbox.Min.X),
                        Math.Min(globalMin.Y, bbox.Min.Y),
                        Math.Min(globalMin.Z, bbox.Min.Z)
                    );

                    // Compare max
                    globalMax = new XYZ(
                        Math.Max(globalMax.X, bbox.Max.X),
                        Math.Max(globalMax.Y, bbox.Max.Y),
                        Math.Max(globalMax.Z, bbox.Max.Z)

                    );
                }

                // Add margin of 10 (Revit units → feet by default)
                double offset = 10.0;

                globalMin = new XYZ(globalMin.X - offset, globalMin.Y - offset, globalMin.Z - offset);
                globalMax = new XYZ(globalMax.X + offset, globalMax.Y + offset, globalMax.Z + offset);

                // Adjust the CropBox
                BoundingBoxXYZ cropBox = view.CropBox;
                if (cropBox == null)
                    cropBox = new BoundingBoxXYZ();

                cropBox.Min = globalMin;
                cropBox.Max = globalMax;

                view.CropBox = cropBox;

                // --- Hide all grids using SetCategoryHidden ---
                Category gridsCategory = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Grids);
                if (view.CanCategoryBeHidden(gridsCategory.Id))
                {
                    view.SetCategoryHidden(gridsCategory.Id, true);
                }

                // --- Hide all generic annotations using SetCategoryHidden ---
                Category annotationsCategory = doc.Settings.Categories.get_Item(BuiltInCategory.OST_GenericAnnotation);
                if (view.CanCategoryBeHidden(annotationsCategory.Id))
                {
                    view.SetCategoryHidden(annotationsCategory.Id, true);
                }

                // --- Hide all section markers using SetCategoryHidden ---
                Category sectionsCategory = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Sections);
                if (view.CanCategoryBeHidden(sectionsCategory.Id))
                {
                    view.SetCategoryHidden(sectionsCategory.Id, true);
                }

                // --- Hide all elevation markers using SetCategoryHidden ---
                Category elevationsCategory = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Elev);
                if (view.CanCategoryBeHidden(elevationsCategory.Id))
                {
                    view.SetCategoryHidden(elevationsCategory.Id, true);
                }


                t.Commit();
            }
        }

        public static void ShowAnnotations(Document doc, SelectionWindow window)
        {
            // --- Show grids, annotations, sections and elevations again in all selected views ---
            using (Transaction tShowCategories = new Transaction(doc, "Show Categories"))
            {
                tShowCategories.Start();

                Category gridsCategory = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Grids);
                Category annotationsCategory = doc.Settings.Categories.get_Item(BuiltInCategory.OST_GenericAnnotation);
                Category sectionsCategory = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Sections);
                Category elevationsCategory = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Elev);

                foreach (var viewId in window.SelectedViewIds)
                {
                    Autodesk.Revit.DB.View view = doc.GetElement(viewId) as Autodesk.Revit.DB.View;
                    if (view == null) continue;

                    // --- Unhide grids ---
                    if (view.CanCategoryBeHidden(gridsCategory.Id))
                    {
                        view.SetCategoryHidden(gridsCategory.Id, false);
                    }

                    // --- Unhide generic annotations ---
                    if (view.CanCategoryBeHidden(annotationsCategory.Id))
                    {
                        view.SetCategoryHidden(annotationsCategory.Id, false);
                    }

                    // --- Unhide section markers ---
                    if (view.CanCategoryBeHidden(sectionsCategory.Id))
                    {
                        view.SetCategoryHidden(sectionsCategory.Id, false);
                    }

                    // --- Unhide elevation markers ---
                    if (view.CanCategoryBeHidden(elevationsCategory.Id))
                    {
                        view.SetCategoryHidden(elevationsCategory.Id, false);
                    }
                }

                tShowCategories.Commit();
            }

        }

    }
}
