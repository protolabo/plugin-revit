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
                string tempFolderPath = Path.Combine(buildFilesDir, "tempFolder");
                string destDir = Path.Combine(tempFolderPath, "Template");
                string wallTypesPath = Path.Combine(destDir, "wallTypes.json");

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

                            // Calculate Attenuation as the average of the attenuationFactor values in propagationProperties
                            var propagationProps = item["propagationProperties"] as JArray;
                            double attenuation = 0.0;
                            if (propagationProps != null && propagationProps.Count > 0)
                            {
                                attenuation = propagationProps
                                    .Select(p => (double?)p["attenuationFactor"] ?? 0.0)
                                    .Average();
                            }

                            wallTypeItems.Add(new WallTypeItem
                            {
                                Name = name,
                                Ekahau = key,
                                CalculatedAttenuation = Math.Round(attenuation, 1)
                            });
                        }

                        this.AvailableWallTypeItems = wallTypeItems;
                        this.AvailableWallTypes = wallTypeItems.Select(item => item.ToString()).ToList();
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
            // Validation: ensure there are no negative attenuation values
            var negativeWalls = walls.Where(w => w.Attenuation < 0).ToList();
            var negativeDoors = doors.Where(d => d.Attenuation < 0).ToList();
            var negativeWindows = windows.Where(w => w.Attenuation < 0).ToList();

            if (negativeWalls.Any() || negativeDoors.Any() || negativeWindows.Any())
            {
                string message = "Some elements have negative attenuation values:\n";

                if (negativeWalls.Any())
                    message += "\nWalls:\n" + string.Join("\n", negativeWalls.Select(w => $"- {w.Revit}: {w.Attenuation} dB"));
                if (negativeDoors.Any())
                    message += "\nDoors:\n" + string.Join("\n", negativeDoors.Select(d => $"- {d.Revit}: {d.Attenuation} dB"));
                if (negativeWindows.Any())
                    message += "\nWindows:\n" + string.Join("\n", negativeWindows.Select(w => $"- {w.Revit}: {w.Attenuation} dB"));

                MessageBox.Show(message, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validation of duplicate Ekahau types with different attenuation values
            var allEntries = new List<(string Tab, WallData Data)>();

            allEntries.AddRange(walls.Select(w => ("Walls", w)));
            allEntries.AddRange(doors.Select(d => ("Doors", d)));
            allEntries.AddRange(windows.Select(wi => ("Windows", wi)));

            var groupedByEkahau = allEntries
                .Where(x => !string.IsNullOrWhiteSpace(x.Data.Ekahau))
                .GroupBy(x => x.Data.Ekahau);

            foreach (var group in groupedByEkahau)
            {
                var distinctAttenuations = group.Select(x => x.Data.Attenuation).Distinct().ToList();
                if (distinctAttenuations.Count > 1)
                {
                    string message = $"The Ekahau type \"{group.Key}\" has different attenuation values:\n\n" +
                                     string.Join("\n", group.Select(x =>
                                         $"- {x.Tab} | {x.Data.Revit} | {x.Data.Attenuation} dB"));

                    MessageBox.Show(message, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            EditedWallData = WallsGrid.SelectedItem as WallData;

            try
            {
                var output = new List<object>
                {
                    new { walls },
                    new { Doors = doors },
                    new { Windows = windows }
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = System.Text.Json.JsonSerializer.Serialize(output, options);
                File.WriteAllText(dataFilePath, json);
            }
            catch
            {
                MessageBox.Show("Error saving data.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

            wallData.Attenuation = wallItem.CalculatedAttenuation;
            wallData.Ekahau = wallItem.Name;
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
            set { _attenuation = value; OnPropertyChanged(nameof(Attenuation)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class WallTypeItem
    {
        public string Name { get; set; }
        public string Ekahau { get; set; }
        public double CalculatedAttenuation { get; set; }

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





