using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using Newtonsoft.Json.Linq;
using System.Windows.Controls;
using System.ComponentModel;

namespace Create
{
    public partial class EditWallsWindow : Window
    {
        private List<WallData> walls;
        private List<WallData> doors;
        private List<WallData> windows;

        public WallData EditedWallData { get; private set; }
        public List<string> AvailableWallTypes { get; set; }
        public List<WallTypeItem> AvailableWallTypeItems { get; set; }

        private string dataFilePath = Path.Combine(
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
            "wall_data.json"
        );

        // Constructor con nuevo ítem para agregar directamente
        public EditWallsWindow(WallData newItem) : this()
        {
            walls.Add(newItem);
            WallsGrid.Items.Refresh();
            WallsGrid.SelectedItem = newItem;
            WallsGrid.ScrollIntoView(newItem);
        }

        public EditWallsWindow()
        {
            InitializeComponent();

            // Leer archivo de datos JSON
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

            // Conectar datos con los DataGrids
            WallsGrid.ItemsSource = walls;
            DoorsGrid.ItemsSource = doors;
            WindowsGrid.ItemsSource = windows;

            // Leer tipos disponibles
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

                    // Parsear como JObject para acceder a la propiedad "wallTypes"
                    var jObject = JObject.Parse(wallTypesJson);
                    var wallTypesArray = jObject["wallTypes"] as JArray;

                    if (wallTypesArray != null)
                    {
                        var wallTypeItems = new List<WallTypeItem>();

                        foreach (var item in wallTypesArray)
                        {
                            string name = item["name"]?.ToString() ?? "";
                            string key = item["key"]?.ToString() ?? "";

                            // Calcular Attenuation como promedio de los attenuationFactor en propagationProperties
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

        private void Save_Click(object sender, RoutedEventArgs e)
        {
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
            var comboBox = sender as ComboBox;
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





