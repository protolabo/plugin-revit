﻿using Autodesk.Revit.UI;
using System;
using System.Reflection;
using System.Windows.Media.Imaging;
using System.IO;

namespace Create
{
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            string tabName = "Ekahau";

            try { application.CreateRibbonTab(tabName); } catch { }

            RibbonPanel toolsPanel = application.CreateRibbonPanel(tabName, "Tools");

            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            string dir = Path.GetDirectoryName(assemblyPath);

            // Load images
            BitmapImage exportIcon = new BitmapImage(new Uri(Path.Combine(dir, "Resources", "export.png")));
            BitmapImage importIcon = new BitmapImage(new Uri(Path.Combine(dir, "Resources", "import.png")));
            //BitmapImage editIcon = new BitmapImage(new Uri(Path.Combine(dir, "Resources", "edit.png"))); // <-- nueva imagen

            // Export button
            PushButtonData exportButtonData = new PushButtonData(
                "btnExport", "Export", assemblyPath, "Create.Export");
            PushButton exportButton = toolsPanel.AddItem(exportButtonData) as PushButton;
            exportButton.ToolTip = "Export to Ekahau (.esx) format";
            exportButton.LargeImage = exportIcon;

            // Import button
            PushButtonData importButtonData = new PushButtonData(
                "btnImport", "Import", assemblyPath, "Create.Import");
            PushButton importButton = toolsPanel.AddItem(importButtonData) as PushButton;
            importButton.ToolTip = "Import from Ekahau format";
            importButton.LargeImage = importIcon;

            // Walls button
            PushButtonData wallsButtonData = new PushButtonData(
                "btnWalls", "Walls", assemblyPath, "Create.EditWalls");
            PushButton wallsButton = toolsPanel.AddItem(wallsButtonData) as PushButton;
            wallsButton.ToolTip = "Edit walls mapping manually";
            //editButton.LargeImage = editIcon;

            // Update Walls button
            PushButtonData updateWallsButtonData = new PushButtonData(
                "btnUpdate", "Update", assemblyPath, "Create.UpdateWalls");
            PushButton updateWallsButton = toolsPanel.AddItem(updateWallsButtonData) as PushButton;
            updateWallsButton.ToolTip = "Look for new type of walls";
            //editButton.LargeImage = editIcon;

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}


