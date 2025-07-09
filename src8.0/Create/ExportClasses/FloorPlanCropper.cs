using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;


namespace Create.ExportClasses
{
    /*
     * This class is responsible for cropping floor plans automatically, containing 
     * all the mandatory elements required to export to the Ekahau file.
     */

    internal class FloorPlanCropper
    {
        public static Result CropFloorPlans(Document doc, List<ElementId> plan_views_ids)
        {
            using (Transaction t = new Transaction(doc, "Adjust view crop regions"))
            {
                t.Start();

                try
                {
                    foreach (var viewId in plan_views_ids)
                    {
                        View view = doc.GetElement(viewId) as ViewPlan;
                        if (view == null) continue;

                        view.CropBoxActive = true;
                        view.CropBoxVisible = true;
                        ProcessViewCrop(doc, view);
                    }

                    t.Commit();
                }
                catch (Exception ex)
                {
                    t.RollBack();
                    TaskDialog.Show("Error", $"Failed to adjust crop regions: {ex.Message}");
                    return Result.Failed;
                }

                return Result.Succeeded;
            }
        }
        /*
         * Here we set the element crop box to the size of the annotation crop box
         * to make sure every element, including annotations would be exported.
         */
        //
        private static void ProcessViewCrop(Document doc, View view)
        {
            BoundingBoxXYZ elementCropBox = view.CropBox;

            BoundingBoxXYZ annotationCropBox = GetAnnotationCropBox(view);

            if (annotationCropBox != null)
            {
                elementCropBox.Min = new XYZ(
                    Math.Min(elementCropBox.Min.X, annotationCropBox.Min.X),
                    Math.Min(elementCropBox.Min.Y, annotationCropBox.Min.Y),
                    Math.Min(elementCropBox.Min.Z, annotationCropBox.Min.Z));

                elementCropBox.Max = new XYZ(
                    Math.Max(elementCropBox.Max.X, annotationCropBox.Max.X),
                    Math.Max(elementCropBox.Max.Y, annotationCropBox.Max.Y),
                    Math.Max(elementCropBox.Max.Z, annotationCropBox.Max.Z));

                view.CropBox = elementCropBox;
            }
        }

        /*
         * This method creates a bounding box for the annotation crop box so that we can
         * compare it to the element crop box. Increasing its size might be needed.
         */
        private static BoundingBoxXYZ GetAnnotationCropBox(View view)
        {
            CurveLoop annotationCropShape = (CurveLoop)view.GetCropRegionShapeManager().GetAnnotationCropShape();
            if (annotationCropShape == null)
                return null;

            BoundingBoxXYZ bbox = new BoundingBoxXYZ();
            bool first = true;

            foreach (Curve curve in annotationCropShape)
            {
                foreach (XYZ point in curve.Tessellate())
                {
                    if (first)
                    {
                        bbox.Min = point;
                        bbox.Max = point;
                        first = false;
                    }
                    else
                    {
                        bbox.Min = new XYZ(
                            Math.Min(bbox.Min.X, point.X),
                            Math.Min(bbox.Min.Y, point.Y),
                            Math.Min(bbox.Min.Z, point.Z));
                        bbox.Max = new XYZ(
                            Math.Max(bbox.Max.X, point.X),
                            Math.Max(bbox.Max.Y, point.Y),
                            Math.Max(bbox.Max.Z, point.Z));
                    }
                }
            }

            // [Experimenting]: we might need to add a margin to make sure we encapsulate every
            // element, without cutting them through cropping.

            //XYZ margin = (bbox.Max - bbox.Min) * 0.085;
            //bbox.Min -= margin;
            //bbox.Max += margin;

            return bbox;
        }
    }
}