using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using Newtonsoft.Json.Linq;
using System.Windows.Controls;
using System.ComponentModel;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using System.Reflection;

namespace Create
{
    public partial class EditWallsWindow : Window
    {
        private List<WallData> walls;
        private List<WallData> doors;
        private List<WallData> windows;

        private ExternalCommandData _commandData;

        public WallData EditedWallData { get; private set; }
        public List<string> AvailableWallTypes { get; set; }
        public List<WallTypeItem> AvailableWallTypeItems { get; set; }

        private string dataFilePath = Path.Combine(
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
            "build_files",
            "build_tools",
            "wall_data.json"
        );

        // Constructor with new item to add directly
        public EditWallsWindow(ExternalCommandData commandData, WallData newItem) : this(commandData)
        {
            walls.Add(newItem);
            WallsGrid.Items.Refresh();
            WallsGrid.SelectedItem = newItem;
            WallsGrid.ScrollIntoView(newItem);
        }

        public EditWallsWindow(ExternalCommandData commandData)
        {
            InitializeComponent();
            _commandData = commandData;

            LoadUsedTypesFromModel(commandData);

            // Read JSON data file
            if (File.Exists(dataFilePath))
            {
                try
                {
                    string json = File.ReadAllText(dataFilePath);
                    var jArray = JArray.Parse(json);

                    var wallsObj = jArray.FirstOrDefault(obj => obj["walls"] != null);
                    var doorsObj = jArray.FirstOrDefault(obj => obj["Doors"] != null);
                    var windowsObj = jArray.FirstOrDefault(obj => obj["Windows"] != null);

                    walls = wallsObj?["walls"]?.ToObject<List<WallData>>() ?? new List<WallData>();
                    doors = doorsObj?["Doors"]?.ToObject<List<WallData>>() ?? new List<WallData>();
                    windows = windowsObj?["Windows"]?.ToObject<List<WallData>>() ?? new List<WallData>();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reading wall_data.json: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    walls = new List<WallData>();
                    doors = new List<WallData>();
                    windows = new List<WallData>();
                }
            }
            else
            {
                walls = new List<WallData>();
                doors = new List<WallData>();
                windows = new List<WallData>();
            }

            // Bind data to the DataGrids
            WallsGrid.ItemsSource = walls;
            DoorsGrid.ItemsSource = doors;
            WindowsGrid.ItemsSource = windows;

            // Read available types from Ekahau JSON file
            try
            {
                string assemblyFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string buildFilesDir = Path.Combine(assemblyFolder, "build_files");
                string destDir = Path.Combine(buildFilesDir, "build_tools");
                string wallTypesPath = Path.Combine(destDir, "wallTypesOriginal.json");

                if (File.Exists(wallTypesPath))
                {
                    string wallTypesJson = File.ReadAllText(wallTypesPath);

                    // Parse as JObject to access the "wallTypes" property
                    var jObject = JObject.Parse(wallTypesJson);
                    var wallTypesArray = jObject["wallTypes"] as JArray;

                    if (wallTypesArray != null)
                    {
                        var wallTypeItems = new List<WallTypeItem>();

                        foreach (var item in wallTypesArray)
                        {
                            string name = item["name"]?.ToString() ?? "";
                            string key = item["key"]?.ToString() ?? "";

                            // Take calculatedAttenuation directly from JSON
                            double calculatedAttenuation = item["calculatedAttenuation"]?.ToObject<double>() ?? 0.0;
                            double assignedAttenuation = item["assignedAttenuation"]?.ToObject<double>() ?? 0.0;

                            wallTypeItems.Add(new WallTypeItem
                            {
                                Name = name,
                                Ekahau = key,
                                CalculatedAttenuation = Math.Round(calculatedAttenuation, 1),
                                AssignedAttenuation = Math.Round(assignedAttenuation, 1)
                            });
                        }

                        this.AvailableWallTypeItems = wallTypeItems;
                        this.AvailableWallTypes = wallTypeItems.Select(item => item.ToString()).ToList();

                        // ---------------------------
                        // Assign assignedAttenuation to each WallData based on Ekahau name
                        // ---------------------------
                        Action<List<WallData>> assignAttenuation = list =>
                        {
                            foreach (var w in list)
                            {
                                var wallType = AvailableWallTypeItems.FirstOrDefault(x => x.Name == w.Ekahau);
                                if (wallType != null)
                                {
                                    w.Attenuation = wallType.AssignedAttenuation; // Assign value from JSON
                                    w.WallTypeReference = wallType;
                                }

                                // Subscribe to PropertyChanged to propagate future changes
                                w.PropertyChanged += Wall_PropertyChanged;
                            }
                        };

                        assignAttenuation(walls);
                        assignAttenuation(doors);
                        assignAttenuation(windows);

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading wallTypes.json: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            this.DataContext = this;
        }

        private void FilterByModelCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            bool isChecked = FilterByModelCheckBox.IsChecked == true;

            if (isChecked)
            {
                // Filter grids to show only elements from this model
                FilterGridsForCurrentModel();
            }
            else
            {
                // Show all elements (remove filter)
                ShowAllElementsInGrids();
            }
        }

        private void FilterGridsForCurrentModel()
        {
            var filteredWalls = walls.Where(w => usedTypeNames.Contains(w.Revit)).ToList();
            var filteredDoors = doors.Where(d => usedTypeNames.Contains(d.Revit)).ToList();
            var filteredWindows = windows.Where(win => usedTypeNames.Contains(win.Revit)).ToList();

            WallsGrid.ItemsSource = filteredWalls;
            DoorsGrid.ItemsSource = filteredDoors;
            WindowsGrid.ItemsSource = filteredWindows;
        }

        private void ShowAllElementsInGrids()
        {
            // Reset the ItemsSource to show all items
            WallsGrid.ItemsSource = walls;
            DoorsGrid.ItemsSource = doors;
            WindowsGrid.ItemsSource = windows;
        }

        private HashSet<string> usedTypeNames = new HashSet<string>();

        private void LoadUsedTypesFromModel(ExternalCommandData commandData)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            // Categories to check
            BuiltInCategory[] filteredCategories = new[]
            {
                BuiltInCategory.OST_Walls,
                BuiltInCategory.OST_Doors,
                BuiltInCategory.OST_Windows
            };

            usedTypeNames.Clear();

            foreach (var category in filteredCategories)
            {
                // Get all instances of this category
                var instances = new FilteredElementCollector(doc)
                                    .OfCategory(category)
                                    .WhereElementIsNotElementType()
                                    .ToElements();

                foreach (var instance in instances)
                {
                    ElementId typeId = instance.GetTypeId();
                    Element typeElement = doc.GetElement(typeId);
                    if (typeElement != null)
                    {
                        string typeName = typeElement.Name;
                        if (!usedTypeNames.Contains(typeName))
                        {
                            usedTypeNames.Add(typeName);
                        }
                    }
                }
            }
        }


        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // ----------------------------
            // Save walls, doors, windows to wallData.json
            // ----------------------------
            try
            {
                var output = new List<object>
                {
                    new { walls = walls.Select(w => new { w.Revit, w.Ekahau }).ToList() },
                    new { Doors = doors.Select(d => new { d.Revit, d.Ekahau }).ToList() },
                    new { Windows = windows.Select(win => new { win.Revit, win.Ekahau }).ToList() }
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = System.Text.Json.JsonSerializer.Serialize(output, options);
                File.WriteAllText(dataFilePath, json);

            }
            catch
            {
                MessageBox.Show("Error saving wallData.json.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // ----------------------------
            // Update assignedAttenuation in wallTypesOriginal.json
            // ----------------------------
            try
            {
                string assemblyFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string buildFilesDir = Path.Combine(assemblyFolder, "build_files");
                string destDir = Path.Combine(buildFilesDir, "build_tools");
                string wallTypesPath = Path.Combine(destDir, "wallTypesOriginal.json");

                if (File.Exists(wallTypesPath))
                {
                    string wallTypesJson = File.ReadAllText(wallTypesPath);
                    var jObject = JObject.Parse(wallTypesJson);
                    var wallTypesArray = jObject["wallTypes"] as JArray;

                    if (wallTypesArray != null)
                    {
                        foreach (var wallType in wallTypesArray)
                        {
                            string name = wallType["name"]?.ToString();
                            if (string.IsNullOrEmpty(name)) continue;

                            // Find any edited WallData with matching Ekahau name
                            var matchingData = walls.Concat(doors).Concat(windows)
                                                    .FirstOrDefault(w => w.Ekahau == name);

                            if (matchingData != null)
                            {
                                double attenuation = matchingData.Attenuation;

                                // Validation: do not allow negative values
                                if (attenuation < 0)
                                {
                                    MessageBox.Show($"Negative attenuation values are not allowed. Wall type: '{name}'.",
                                                    "Error",
                                                    MessageBoxButton.OK,
                                                    MessageBoxImage.Error);
                                    return; // stop saving process
                                }

                                wallType["assignedAttenuation"] = attenuation;
                            }

                        }

                        // Save updated wallTypesOriginal.json
                        File.WriteAllText(wallTypesPath, jObject.ToString(Newtonsoft.Json.Formatting.Indented));
                    }
                }
            }
            catch
            {
                MessageBox.Show("Error updating wallTypesOriginal.json.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            this.DialogResult = true;
            this.Close();
        }



        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var selectedTab = ((TabControl)Content).SelectedItem as TabItem;
            if (selectedTab == null) return;

            if (selectedTab.Header.ToString() == "Walls" && WallsGrid.SelectedItem is WallData wall)
            {
                ConfirmAndDelete(wall, walls, WallsGrid);
            }
            else if (selectedTab.Header.ToString() == "Doors" && DoorsGrid.SelectedItem is WallData door)
            {
                ConfirmAndDelete(door, doors, DoorsGrid);
            }
            else if (selectedTab.Header.ToString() == "Windows" && WindowsGrid.SelectedItem is WallData window)
            {
                ConfirmAndDelete(window, windows, WindowsGrid);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); 
        }

        private void ConfirmAndDelete(WallData item, List<WallData> list, DataGrid grid)
        {
            var result = MessageBox.Show($"Are you sure you want to delete '{item.Revit}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                list.Remove(item);
                grid.Items.Refresh();
            }
        }

        private void EkahauCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as System.Windows.Controls.ComboBox;
            if (comboBox == null) return;

            var selectedName = comboBox.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedName)) return;

            var wallItem = AvailableWallTypeItems.FirstOrDefault(x => selectedName.StartsWith(x.Name));
            if (wallItem == null) return;

            var wallData = comboBox.DataContext as WallData;
            if (wallData == null) return;

            wallData.Attenuation = wallItem.AssignedAttenuation;
            wallData.Ekahau = wallItem.Name;
        }

        private void Wall_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var wall = sender as WallData;
            if (wall == null || string.IsNullOrEmpty(wall.Ekahau)) return;

            if (e.PropertyName == nameof(WallData.Attenuation))
            {
                // Update all rows with the same Ekahau type
                foreach (var w in walls.Where(x => x.Ekahau == wall.Ekahau && x != wall))
                {
                    w.Attenuation = wall.Attenuation; // This will trigger OnPropertyChanged for each row
                }
                foreach (var d in doors.Where(x => x.Ekahau == wall.Ekahau))
                {
                    d.Attenuation = wall.Attenuation;
                }
                foreach (var win in windows.Where(x => x.Ekahau == wall.Ekahau))
                {
                    win.Attenuation = wall.Attenuation;
                }

                // Also update the corresponding WallTypeItem
                var wallType = AvailableWallTypeItems.FirstOrDefault(x => x.Name == wall.Ekahau);
                if (wallType != null)
                {
                    wallType.CalculatedAttenuation = wall.Attenuation;
                }
            }

            // Optional: if you want to propagate Ekahau name changes too, you can add similar logic here
        }


    }

    public class WallData : INotifyPropertyChanged
    {
        private string _revit;
        private string _ekahau;
        private double _attenuation;

        public string Revit
        {
            get => _revit;
            set { _revit = value; OnPropertyChanged(nameof(Revit)); }
        }

        public string Ekahau
        {
            get => _ekahau;
            set { _ekahau = value; OnPropertyChanged(nameof(Ekahau)); }
        }

        public double Attenuation
        {
            get => _attenuation;
            set
            {
                if (_attenuation != value)
                {
                    _attenuation = value;
                    OnPropertyChanged(nameof(Attenuation));
                    UpdateWallTypeItem();
                }
            }
        }

        // Reference to the corresponding WallTypeItem
        public WallTypeItem WallTypeReference { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void UpdateWallTypeItem()
        {
            if (WallTypeReference != null)
            {
                WallTypeReference.CalculatedAttenuation = _attenuation;
            }
        }
    }

    public class WallTypeItem
    {
        public string Name { get; set; }
        public string Ekahau { get; set; }
        public double CalculatedAttenuation { get; set; }
        public double AssignedAttenuation { get; set; }

        public override string ToString()
        {
            return $"{Name} ({CalculatedAttenuation:F1} dB)";
        }
    }

    public class WallJsonItem
    {
        public string Revit { get; set; }
        public string Ekahau { get; set; }
        public double Attenuation { get; set; }
    }
}





