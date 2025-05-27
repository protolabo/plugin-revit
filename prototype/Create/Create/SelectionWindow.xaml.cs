using System.Collections.Generic;
using System.Windows;
using Autodesk.Revit.DB;  

namespace Create
{
    public partial class SelectionWindow : Window
    {
        public List<int> SelectedCategories { get; private set; } = new List<int>();

        public SelectionWindow()
        {
            InitializeComponent();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            SelectedCategories.Clear();

            if (chkWalls.IsChecked == true)
                SelectedCategories.Add((int)BuiltInCategory.OST_Walls);
            if (chkDoors.IsChecked == true)
                SelectedCategories.Add((int)BuiltInCategory.OST_Doors);
            if (chkWindows.IsChecked == true)
                SelectedCategories.Add((int)BuiltInCategory.OST_Windows);

            this.DialogResult = true;
            this.Close();
        }
    }
}

