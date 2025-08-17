using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Create.ExportClasses
{
    internal class ImageExporter
    {
        public static void CreateViewImagesAndReport(
            ExternalCommandData commandData,
            string outputDir,
            List<ElementId> selectedViewIds,
            List<ViewData> viewInfo) 
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            string tempExportDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "build_files", "tempFolder");
            Directory.CreateDirectory(tempExportDir);

            string imageBasePath = Path.Combine(tempExportDir, "exported_view");

            foreach (var viewId in selectedViewIds)
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
                    ZoomType = ZoomFitType.FitToPage,
                    PixelSize = 1500
                };
                imageOptions.SetViewsAndSheets(new List<ElementId>() { view.Id });

                doc.ExportImage(imageOptions);

                // Find the most recent exported file that contains the view name to add its information 
                // to the json file that contains the information for all exported images
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

                viewInfo.Add(new ViewData
                {
                    viewName = viewName,
                    min = new Point
                    {
                        x = min?.X ?? 0,
                        y = min?.Y ?? 0,
                        z = min?.Z ?? 0
                    },
                    max = new Point
                    {
                        x = max?.X ?? 0,
                        y = max?.Y ?? 0,
                        z = max?.Z ?? 0
                    },
                    width = width,
                    height = height
                });
            }
        }

    }
}




