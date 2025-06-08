using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Create
{
    public partial class SelectionWindow : Window
    {
        private readonly Document _doc;

        public List<int> SelectedCategories { get; private set; } = new List<int>();
        public List<ElementId> SelectedViewIds { get; private set; } = new List<ElementId>();

        public SelectionWindow(Document doc)
        {
            InitializeComponent();
            _doc = doc;
            Loaded += SelectionWindow_Loaded;
        }

        private void SelectionWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Filter only views of type FloorPlan
            var views = new FilteredElementCollector(_doc)
                            .OfClass(typeof(ViewPlan))
                            .Cast<ViewPlan>()
                            .Where(v => v.ViewType == ViewType.FloorPlan && !v.IsTemplate) // Filter only floor plan views, excluding view templates
                            .OrderBy(v => v.Name);

            foreach (var view in views)
            {
                ViewsListBox.Items.Add(new CheckBox
                {
                    Content = view.Name,
                    Tag = view.Id
                });
            }
        }


        private void OK_Click(object sender, RoutedEventArgs e)
        {
            SelectedCategories.Clear();
            if (chkStairs.IsChecked == true)
                SelectedCategories.Add((int)BuiltInCategory.OST_Stairs);

            SelectedViewIds.Clear();
            foreach (CheckBox cb in ViewsListBox.Items)
            {
                if (cb.IsChecked == true)
                    SelectedViewIds.Add((ElementId)cb.Tag);
            }

            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}




