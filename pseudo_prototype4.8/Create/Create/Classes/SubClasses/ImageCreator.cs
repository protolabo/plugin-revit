using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Create.Helpers.SubFunctions
{
    internal class ImageCreator
    {
        public static void PrepareImageAndFiles(ExternalCommandData commandData, string destDir, List<ElementId> selectedViews)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            string tempExportDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "build_files", "build_tools");
            Directory.CreateDirectory(tempExportDir);

            string imageBasePath = Path.Combine(tempExportDir, "exported_view");
            string jsonOutputPath = Path.Combine(tempExportDir, "imageData.json");

            JArray imageInfoArray = new JArray();

            foreach (var viewId in selectedViews)
            {
                View view = doc.GetElement(viewId) as View;
                if (view == null) continue;

                string viewName = view.Name;

                // Export image
                ImageExportOptions imageOptions = new ImageExportOptions
                {
                    ExportRange = ExportRange.SetOfViews,
                    HLRandWFViewsFileType = ImageFileType.BMP,
                    FilePath = imageBasePath,
                    ZoomType = ZoomFitType.Zoom,
                    Zoom = 100  // Escala 1:1
                };
                imageOptions.SetViewsAndSheets(new List<ElementId>() { view.Id });

                doc.ExportImage(imageOptions);

                // Buscar el archivo exportado más reciente que contenga el nombre de la vista
                var matchingImage = Directory.GetFiles(tempExportDir, $"exported_view - *{viewName}*.bmp")
                    .OrderByDescending(File.GetLastWriteTime)
                    .FirstOrDefault();

                if (matchingImage == null) continue;

                int width = 0, height = 0;
                using (var bmp = new Bitmap(matchingImage))
                {
                    width = bmp.Width;
                    height = bmp.Height;
                }

                XYZ min = null, max = null;
                if (view.CropBox != null)
                {
                    BoundingBoxXYZ cropBox = view.CropBox;
                    min = cropBox.Min;
                    max = cropBox.Max;
                }

                JObject imageObj = new JObject
                {
                    ["viewName"] = viewName,
                    ["min"] = new JObject
                    {
                        ["x"] = min?.X,
                        ["y"] = min?.Y,
                        ["z"] = min?.Z
                    },
                    ["max"] = new JObject
                    {
                        ["x"] = max?.X,
                        ["y"] = max?.Y,
                        ["z"] = max?.Z
                    },
                    ["width"] = width,
                    ["height"] = height
                };

                imageInfoArray.Add(imageObj);
            }

            File.WriteAllText(jsonOutputPath, imageInfoArray.ToString());
        }
    }
}




