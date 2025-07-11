using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Windows.Controls;
using System.ComponentModel;


namespace Create
{
    public partial class EditWallsWindow : Window
    {
        private List<WallData> walls;

        public WallData EditedWallData { get; private set; }
        public List<string> AvailableWallTypes { get; set; }
        public List<WallTypeItem> AvailableWallTypeItems { get; set; }

        private string dataFilePath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "wall_data.json");

        // New constructor that accepts an initial item to add/edit
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

            if (File.Exists(dataFilePath))
            {
                try
                {
                    string json = File.ReadAllText(dataFilePath);
                    walls = System.Text.Json.JsonSerializer.Deserialize<List<WallData>>(json);
                }
                catch
                {
                    walls = new List<WallData>();
                }
            }
            else
            {
                walls = new List<WallData>();
            }

            WallsGrid.ItemsSource = walls;

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
                    var wallTypesJObject = Newtonsoft.Json.Linq.JObject.Parse(wallTypesJson);

                    var wallTypeItems = wallTypesJObject["wallTypes"]
                        .Select(w =>
                        {
                            string name = (string)w["name"];

                            // first attenuationFactor
                            var propagationProperties = w["propagationProperties"]?.FirstOrDefault();
                            double attenuationFactor = propagationProperties?["attenuationFactor"]?.Value<double>() ?? 0.0;

                            // thickness
                            double thickness = w["thickness"]?.Value<double>() ?? 1.0;

                            double totalAttenuation = Math.Round(attenuationFactor * thickness, 1);

                            return new WallTypeItem
                            {
                                Name = name,
                                CalculatedAttenuation = totalAttenuation
                            };
                        })
                        .ToList();

                    this.AvailableWallTypes = wallTypeItems
                        .Select(item => item.ToString())
                        .ToList();

                    this.AvailableWallTypeItems = wallTypeItems;
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
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = System.Text.Json.JsonSerializer.Serialize(walls, options);
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
            if (WallsGrid.SelectedItem is WallData selectedDoor)
            {
                var result = MessageBox.Show($"Are you sure you want to delete '{selectedDoor.Revit}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    walls.Remove(selectedDoor);
                    WallsGrid.Items.Refresh();
                }
            }
            else
            {
                MessageBox.Show("Please select a record to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        private void EkahauCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            var selectedName = comboBox.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedName)) return;

            var wallItem = AvailableWallTypeItems
                .FirstOrDefault(x => selectedName.StartsWith(x.Name));
            if (wallItem == null) return;

            var wallData = comboBox.DataContext as WallData;
            if (wallData == null) return;

            wallData.Structural = wallItem.CalculatedAttenuation;
            wallData.Architectural = wallItem.CalculatedAttenuation;

            wallData.Ekahau = wallItem.Name;

            // WallsGrid.Items.Refresh(); 
        }


    }

    public class WallData : INotifyPropertyChanged
    {
        private string _revit;
        private string _ekahau;
        private double _structural;
        private double _architectural;

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

        public double Structural
        {
            get => _structural;
            set { _structural = value; OnPropertyChanged(nameof(Structural)); }
        }

        public double Architectural
        {
            get => _architectural;
            set { _architectural = value; OnPropertyChanged(nameof(Architectural)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class WallTypeItem
    {
        public string Name { get; set; }
        public double CalculatedAttenuation { get; set; }

        public override string ToString()
        {
            return $"{Name} ({CalculatedAttenuation:F1} dB)";
        }
    }


}




