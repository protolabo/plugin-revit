using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Windows.Forms;
using System.IO.Compression;

// using System.Reflection;
using Create.Helpers;


namespace Create
{
    [Transaction(TransactionMode.ReadOnly)]
    public class Class1 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            ExportInfo.ProcessElements(commandData);

            string chosenFileName = null;

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string destDir = Path.Combine(desktopPath, "myCopy");

            Result result;
            
            result = CopyFolder.PrepareFolder(ref chosenFileName, ref message, destDir);
            if (result != Result.Succeeded) return result;

            string baseName = Path.GetFileNameWithoutExtension(chosenFileName);

            try
            {

                result = CreateID.GenerateIDInFiles(destDir);
                if (result != Result.Succeeded) return result;

                result = UpdateID.CompleteJsonReferences(destDir, baseName);
                if (result != Result.Succeeded) return result;

                //ImageCreator.PrepareImageAndFiles(commandData, destDir);
                result = ImagesJson.ProcessExportedBmp(destDir);
                if (result != Result.Succeeded) return result;

                result = Building.CreateBuilding(destDir);
                if (result != Result.Succeeded) return result;

                result = BuildingJson.CreateBuildingFloorsJson(destDir);
                if (result != Result.Succeeded) return result;

                // Obtener el documento activo
                UIDocument uiDoc = commandData.Application.ActiveUIDocument;
                Document doc = uiDoc.Document;

                result = AddWalls.CreateWalls(doc);
                if (result != Result.Succeeded) return result;

                // CREATE ZIP FILE AND CHANGE FILKE EXTENSION
                // At this point, all the JSON files and the image are ready to create the final ESX(Ekahau) file.
                // The only step left is to create a compressed(zip) file with all the files and change the extension from .zip to .esx.

                result = ExportEkahau.CreateEsx(chosenFileName);
                if (result != Result.Succeeded) return result;

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }

        }

    }
}