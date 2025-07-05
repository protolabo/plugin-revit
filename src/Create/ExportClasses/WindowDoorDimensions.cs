using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace Create.ExportClasses
{
    internal class WindowDoorDimensions
    {
        private static string dataFilePath = Path.Combine(
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
            "door_data.json");

        public static object GetWindowDoorDimensions(FamilyInstance inst, double wallX1, double wallY1, double wallZ1, double wallX2, double wallY2, double wallZ2)
        {
            double width = 3.2;  // default width in feet
            double height = 6.0; // default height in feet

            // Step 1: Search for width and height parameters in the instance
            Parameter widthParam = inst.LookupParameter("Width");
            Parameter heightParam = inst.LookupParameter("Height");

            bool foundWidth = (widthParam != null && widthParam.HasValue);
            bool foundHeight = (heightParam != null && heightParam.HasValue);

            if (foundWidth)
                width = widthParam.AsDouble();

            if (foundHeight)
                height = heightParam.AsDouble();

            // Step 2: If missing, try to get from JSON data
            //if (!foundWidth || !foundHeight)
            //{
            //    var doorDataList = LoadDoorDataFromJson();

            //    DoorData foundData = doorDataList?.Find(d => d.Name == inst.Name);

            //    if (foundData != null)
            //    {
            //        if (!foundWidth && foundData.Width > 0)
            //            width = foundData.Width;

            //        if (!foundHeight && foundData.Height > 0)
            //            height = foundData.Height;

            //        foundWidth = foundWidth || foundData.Width > 0;
            //        foundHeight = foundHeight || foundData.Height > 0;
            //    }
            //}

            // Step 3: If any are still missing, ask the user to enter them.
            //if (!foundWidth || !foundHeight)
            //{
            //    MessageBoxResult res = MessageBox.Show(
            //        $"Las dimensiones del elemento '{inst.Name}' no fueron encontradas.\n¿Deseas agregarlas manualmente?",
            //        "Faltan dimensiones",
            //        MessageBoxButton.OKCancel,
            //        MessageBoxImage.Warning);

            //    if (res == MessageBoxResult.OK)
            //    {
            //        // Create a new empty record with the name.
            //        var newItem = new Create.DoorData
            //        {
            //            Name = inst.Name,
            //            Width = foundWidth ? width : 0,
            //            Height = foundHeight ? height : 0,
            //            Thickness = 0
            //        };

            //        // Show a window for the user to enter or correct data.
            //        var dialog = new Create.EditOpeningsWindow(newItem);
            //        bool? dialogResult = dialog.ShowDialog();

            //        if (dialogResult == true)
            //        {
            //            // Get the updated object directly from the window.
            //            var updatedData = dialog.EditedDoorData;

            //            width = updatedData.Width > 0 ? updatedData.Width : width;
            //            height = updatedData.Height > 0 ? updatedData.Height : height;

            //            // Optional: immediately save to JSON for persistence.
            //            SaveDoorDataToJson(updatedData);
            //        }
            //    }
            //}

            // Step 4: Determine the central position of the element
            XYZ center = null;
            object position = null;
            if (inst.Location is LocationPoint lp)
            {
                center = lp.Point;
            }
            else if (inst.Location is LocationCurve lc)
            {
                center = lc.Curve.Evaluate(0.5, true);
            }
            else
            {
                var bbox = inst.get_BoundingBox(null);
                if (bbox != null)
                {
                    center = (bbox.Min + bbox.Max) / 2;
                }
            }

            // IMPORTANT!!! Add code for diagonal Walls.
            if (center != null)
            {
                double x = center.X;
                double y = center.Y;
                double z = center.Z;

                // Align X if it's constant in the wall
                if (Math.Abs(wallX1 - wallX2) < 0.01)
                    x = wallX1;

                // Align y if it's constant in the wall
                if (Math.Abs(wallY1 - wallY2) < 0.01)
                    y = wallY1;

                center = new XYZ(x, y, z);
                position = new { x, y, z };
            }

            // Step 5: Calculate start and end points
            object start_point = null;
            object end_point = null;

            if (center != null)
            {
                XYZ direction = inst.FacingOrientation?.Normalize();
                if (direction != null)
                {
                    XYZ ortho = new XYZ(-direction.Y, direction.X, direction.Z);
                    double halfWidth = width / 2.0;
                    XYZ offset = ortho.Multiply(halfWidth);

                    XYZ start = center - offset;
                    XYZ end = center + offset;

                    if (Math.Abs(wallX1 - wallX2) < 0.01)
                    {
                        start = new XYZ(wallX1, start.Y, start.Z);
                        end = new XYZ(wallX1, end.Y, end.Z);
                    }
                    if (Math.Abs(wallY1 - wallY2) < 0.01)
                    {
                        start = new XYZ(start.X, wallY1, start.Z);
                        end = new XYZ(end.X, wallY1, end.Z);
                    }

                    start_point = new { x = start.X, y = start.Y, z = start.Z };
                    end_point = new { x = end.X, y = end.Y, z = end.Z };
                }
            }

            // Step 6: Return result
            return new
            {
                type = inst.Category?.Name ?? "Unknown",
                id = inst.Id.IntegerValue,
                name = inst.Name,
                position,
                width_ft = width,
                height_ft = height,
                start_point,
                end_point
            };
        }

        private static List<Create.WallData> LoadDoorDataFromJson()
        {
            try
            {
                if (File.Exists(dataFilePath))
                {
                    string json = File.ReadAllText(dataFilePath);
                    return JsonSerializer.Deserialize<List<Create.WallData>>(json);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading JSON from {dataFilePath}: {ex.Message}");
            }
            return null;
        }

        private static void SaveWallDataToJson(Create.WallData newData)
        {
            try
            {
                List<Create.WallData> list = LoadDoorDataFromJson() ?? new List<Create.WallData>();

                var existing = list.Find(d => d.Revit == newData.Revit);
                if (existing != null)
                {
                    existing.Ekahau = newData.Ekahau;
                    existing.Structural = newData.Structural;
                    existing.Architectural = newData.Architectural;
                }
                else
                {
                    list.Add(newData);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(list, options);
                File.WriteAllText(dataFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving JSON to {dataFilePath}: {ex.Message}");
            }
        }
    }
}

