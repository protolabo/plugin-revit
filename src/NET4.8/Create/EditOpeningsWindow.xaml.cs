using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using Newtonsoft.Json;

namespace Create
{
    public partial class EditOpeningsWindow : Window
    {
        private List<DoorData> openings;

        public DoorData EditedDoorData { get; private set; }

        private string dataFilePath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "door_data.json");

        // New constructor that accepts an initial item to add/edit
        public EditOpeningsWindow(DoorData newItem) : this()
        {
            openings.Add(newItem);
            OpeningsGrid.Items.Refresh();
            OpeningsGrid.SelectedItem = newItem;
            OpeningsGrid.ScrollIntoView(newItem);
        }

        public EditOpeningsWindow()
        {
            InitializeComponent();

            if (File.Exists(dataFilePath))
            {
                try
                {
                    string json = File.ReadAllText(dataFilePath);
                    openings = System.Text.Json.JsonSerializer.Deserialize<List<DoorData>>(json);
                }
                catch
                {
                    openings = new List<DoorData>();
                }
            }
            else
            {
                openings = new List<DoorData>();
            }

            OpeningsGrid.ItemsSource = openings;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            EditedDoorData = OpeningsGrid.SelectedItem as DoorData;

            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = System.Text.Json.JsonSerializer.Serialize(openings, options);
                File.WriteAllText(dataFilePath, json);
            }
            catch
            {
                MessageBox.Show("Error guardando los datos.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            this.DialogResult = true;
            this.Close();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (OpeningsGrid.SelectedItem is DoorData selectedDoor)
            {
                var result = MessageBox.Show($"Are you sure you want to delete '{selectedDoor.Name}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    openings.Remove(selectedDoor);
                    OpeningsGrid.Items.Refresh();
                }
            }
            else
            {
                MessageBox.Show("Please select a record to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }
    }

    public class DoorData
    {
        public string Name { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Thickness { get; set; }
    }

}




