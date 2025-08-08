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
                        // HIDING ANNOTATIONS
                        int hiddenCategories = 0;
                        hiddenCategories = HideAnnotationCategories(doc, viewId);
                        // HIDING ANNOTATIONS

                        ViewPlan? view = doc.GetElement(viewId) as ViewPlan;
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


        // HIDING ANNOTATIONS CODE STARTS
        private static int HideAnnotationCategories(Document doc, ElementId view_id)
        {
            ViewPlan? view = doc.GetElement(view_id) as ViewPlan;
            int hiddenCount = 0;

            // (hopefully) Exhaustive list of ALL annotation categories
            List<BuiltInCategory> allAnnotationCategories = new List<BuiltInCategory>
        {
            // === BASIC ANNOTATIONS ===
            BuiltInCategory.OST_TextNotes,
            BuiltInCategory.OST_KeynoteTags,
            BuiltInCategory.OST_GenericAnnotation,
            BuiltInCategory.OST_Tags,
            
            // === DIMENSIONS ===
            BuiltInCategory.OST_Dimensions,
            BuiltInCategory.OST_SpotElevations,
            BuiltInCategory.OST_SpotCoordinates,
            BuiltInCategory.OST_SpotSlopes,
            BuiltInCategory.OST_MultiReferenceAnnotations,
            
            // === DETAILS AND SYMBOLS ===
            BuiltInCategory.OST_DetailComponents,
            BuiltInCategory.OST_DetailComponentTags,
            BuiltInCategory.OST_GenericModel, // Can contain annotations
            BuiltInCategory.OST_GenericModelTags,
            
            // === LINES ===
            BuiltInCategory.OST_Lines,
            BuiltInCategory.OST_SketchLines,
            BuiltInCategory.OST_CLines,
            BuiltInCategory.OST_CenterLines,
            BuiltInCategory.OST_HiddenLines,
            //BuiltInCategory.OST_MediumLines,
            //BuiltInCategory.OST_WideLines,
            
            // === SPECIALIZED ANNOTATIONS ===
            //BuiltInCategory.OST_SectionMarks,
            BuiltInCategory.OST_ElevationMarks,
            BuiltInCategory.OST_CalloutHeads,
            //BuiltInCategory.OST_ViewTitles,
            BuiltInCategory.OST_ViewportLabel,
            BuiltInCategory.OST_Views,
            
            // === GRILLS AND LEVELS ===
            BuiltInCategory.OST_Grids,
            BuiltInCategory.OST_GridHeads,
            BuiltInCategory.OST_Levels,
            BuiltInCategory.OST_LevelHeads,
            BuiltInCategory.OST_ProjectBasePoint,
            //BuiltInCategory.OST_SurveyPoint,
            
            // === REVISIONS ===
            BuiltInCategory.OST_RevisionClouds,
            BuiltInCategory.OST_RevisionCloudTags,
            
            // === MEP ANNOTATIONS ===
            BuiltInCategory.OST_DuctTags,
            BuiltInCategory.OST_PipeTags,
            BuiltInCategory.OST_CableTrayTags,
            BuiltInCategory.OST_ConduitTags,
            BuiltInCategory.OST_ElectricalFixtureTags,
            BuiltInCategory.OST_LightingFixtureTags,
            BuiltInCategory.OST_MechanicalEquipmentTags,
            BuiltInCategory.OST_PlumbingFixtureTags,
            BuiltInCategory.OST_ElectricalEquipmentTags,
            BuiltInCategory.OST_CommunicationDeviceTags,
            BuiltInCategory.OST_DataDeviceTags,
            BuiltInCategory.OST_FireAlarmDeviceTags,
            BuiltInCategory.OST_LightingDeviceTags,
            BuiltInCategory.OST_NurseCallDeviceTags,
            BuiltInCategory.OST_SecurityDeviceTags,
            BuiltInCategory.OST_TelephoneDeviceTags,
            
            // === STRUCTURAL ANNOTATIONS ===
            BuiltInCategory.OST_StructuralFramingTags,
            BuiltInCategory.OST_StructuralColumnTags,
            BuiltInCategory.OST_StructuralFoundationTags,
            BuiltInCategory.OST_RebarTags,
            //BuiltInCategory.OST_StructuralConnectionTags,
            BuiltInCategory.OST_FabricAreaTags,
            BuiltInCategory.OST_FabricReinforcementTags,
            //BuiltInCategory.OST_PathReinforcementTags,
            //BuiltInCategory.OST_AreaReinforcementTags,
            
            // === ARCHITECTURAL ANNOTATIONS ===
            BuiltInCategory.OST_DoorTags,
            BuiltInCategory.OST_WindowTags,
            BuiltInCategory.OST_WallTags,
            BuiltInCategory.OST_RoomTags,
            BuiltInCategory.OST_AreaTags,
            BuiltInCategory.OST_FloorTags,
            BuiltInCategory.OST_CeilingTags,
            BuiltInCategory.OST_RoofTags,
            BuiltInCategory.OST_StairsTags,
            BuiltInCategory.OST_RailingSystemTags,
            BuiltInCategory.OST_CurtainWallPanelTags,
            BuiltInCategory.OST_CurtainWallMullionTags,
            BuiltInCategory.OST_SpecialityEquipmentTags,
            BuiltInCategory.OST_FurnitureSystemTags,
            BuiltInCategory.OST_FurnitureTags,
            BuiltInCategory.OST_CaseworkTags,
            BuiltInCategory.OST_PlantingTags,
            
            // === SHEETS AND SCHEDULES ===
            BuiltInCategory.OST_Schedules,
            BuiltInCategory.OST_TitleBlocks,
            BuiltInCategory.OST_ScheduleGraphics,
            
            // === OTHER ANNOTATIONS ===
            BuiltInCategory.OST_Matchline,
            //BuiltInCategory.OST_ScopeBoxes,
            BuiltInCategory.OST_ReferencePoints,
            BuiltInCategory.OST_ColorFillLegends,
            //BuiltInCategory.OST_ColorFillSchemas,
            BuiltInCategory.OST_BuildingPad,
            BuiltInCategory.OST_Site,
            BuiltInCategory.OST_Property,
            BuiltInCategory.OST_Viewports,
            BuiltInCategory.OST_ImportObjectStyles,
            
            // === SPECIAL ANNOTATIONS ===
            BuiltInCategory.OST_Fascia,
            BuiltInCategory.OST_Gutter,
            BuiltInCategory.OST_EdgeSlab,
            BuiltInCategory.OST_RoofSoffit,
            BuiltInCategory.OST_Entourage,
            BuiltInCategory.OST_Parking,
            BuiltInCategory.OST_Roads,
            BuiltInCategory.OST_Topography,
            
            // === TEMPORARY ANNOTATIONS ===
            //BuiltInCategory.OST_TemporaryDimensions,
            
            // === MASS ANNOTATIONS ===
            BuiltInCategory.OST_MassTags,
            BuiltInCategory.OST_Mass
        };

            foreach (BuiltInCategory category in allAnnotationCategories)
            {
                // We check whether the category is hideable and whether it is already
                // hidden or not
                try
                {
                    Category cat = doc.Settings.Categories.get_Item(category);
                    if (cat != null && view.CanCategoryBeHidden(cat.Id))
                    {
                        if (!view.GetCategoryHidden(cat.Id))
                        {
                            view.SetCategoryHidden(cat.Id, true);
                            hiddenCount++;
                        }
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }

            return hiddenCount;
        }
    }
    // HIDING ANNOTATIONS CODE ENDS


}
