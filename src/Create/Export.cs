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
using Create.ExportClasses;
using System.Reflection;

namespace Create
{
    [Transaction(TransactionMode.ReadOnly)]
    public class Export : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Dictionary<string, ModelData> modelData = new Dictionary<string, ModelData>();
            Dictionary<string, ModelData> modelDataSegments = new Dictionary<string, ModelData>();

            // The 'ModelData.ExportModelData' function generates a JSON file containing information
            // about the model's walls, windows, and doors.
            // - Use the WindowDoorDimensions.GetWindowDoorDimensions function to obtain the dimensions of elements
            //      embedded in the walls, such as doors and windows
            // - Once the json file has been created, this function uses the WallSplitter.SplitWallByOpening function to split
            //      the walls into sections according to the openings in the wall.
            // - Finally, this function uses the ImageCreator.PrepareImageAndFiles function to export the selected views
            //      as BMP images and creates a JSON file that contains the information of those images.
            // ModelDataExporter.ExportModelData(commandData); (old version - JSON files)
            ModelDataExporter.ExportModelData(commandData, modelData, modelDataSegments);

            string chosenFileName = null;

            string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string buildFilesDir = Path.Combine(assemblyFolder, "build_files");
            string tempFolderPath = Path.Combine(buildFilesDir, "tempFolder");

            // myCopy will contain all the JSON files for the creation of the Ekahau zip.
            string destDir = Path.Combine(tempFolderPath, "Template");

            Result result;

            // The CopyFolder.PrepareFolder function displays a dialog box that allows the user to choose
            // the name and location of the Ekahau file. It then copies the basic files required for creating
            // the Ekahau file. These files, stored in the 'monTemplate' folder, contain only the common information
            // shared by all Ekahau files. The missing information will be filled in progressively during the execution of the code.
            result = FileTemplateCreator.CreateFileTemplate(ref chosenFileName, destDir);
            if (result != Result.Succeeded) return result;

            // Extracts the file name without its extension from the full path selected by the user.
            // For example, "C:\Documents\Plan.esk" becomes "Plan".
            string baseName = Path.GetFileNameWithoutExtension(chosenFileName);

            try
            {
                // // In Ekahau, every object has a unique ID that varies between files, even for identical objects
                // (e.g., "Concrete Wall", which contains all the information about this type of wall).
                // The CreateID.GenerateIDInFiles function generates new IDs for all objects found in the base template files.
                result = IDGenerator.GenerateIDInJsonFiles(destDir);
                if (result != Result.Succeeded) return result;

                // Some files require more than just a new ID. For example, 'projectHistory.json' needs timestamp values to record creation dates.
                // Also, certain files reference IDs defined elsewhere. For instance, 'project.json' sets the project ID,
                // and 'projectHistory.json' uses that ID to associate history data with the project.
                // The function 'UpdateID.CompleteJsonReferences' fills in these additional references and data.
                result = IDReferenceFiller.CompleteJsonReferences(destDir, baseName);
                if (result != Result.Succeeded) return result;

                //ImageCreator.PrepareImageAndFiles(commandData, destDir);
                // The 'ImagesJson.ProcessExportedBmp' function formats the exported images and copies them
                // to the folder that will contain the other JSON files for the Ekahau project.
                // It then updates the 'images.json' file with the required image metadata.
                result = ImageJsonFileCreator.FormatImagesAndCreateJsonFile(destDir, modelData);
                if (result != Result.Succeeded) return result;

                // The 'Building.CreateBuilding' function generates the 'buildings.json' file,
                // which allows the views to appear in the right-hand sidebar within Ekahau.
                result = EkahauBuildingCreator.CreateEkahauBuildingFile(destDir);
                if (result != Result.Succeeded) return result;

                // The 'BuildingJson.CreateBuildingFloorsJson' function generates the 'buildingFloors.json' file,
                // containing information about each floor (view in Revit) of the building for Ekahau.
                result = BuildingFloorCreator.CreateBuildingFloorsJsonFiles(destDir);
                if (result != Result.Succeeded) return result;

                UIDocument uiDoc = commandData.Application.ActiveUIDocument;
                Document doc = uiDoc.Document;

                result = AttenuationUpdater.UpdateEkahauValues();
                if (result != Result.Succeeded) return result;

                // The 'AddWalls.CreateWalls' function generates all the necessary JSON files containing
                // information about the model's walls, including windows and doors,
                // as well as the zones where the simulation will be performed.
                result = WallsInserter.InsertWallAndOpeningsInEkahauFile(doc, modelDataSegments);
                if (result != Result.Succeeded) return result;

                // CREATE ZIP FILE AND CHANGE FILKE EXTENSION
                // At this point, all the JSON files and the image are ready to create the final ESX(Ekahau) file.
                // The only step left is to create a compressed(zip) file with all the files and change the extension from .zip to .esx.
                result = EkahauFileCreator.CreateEsxFile(chosenFileName);
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