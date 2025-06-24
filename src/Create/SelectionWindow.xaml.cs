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

        // New: Dictionary that stores stairs selection per view
        public Dictionary<ElementId, bool> SelectedViewStairs { get; private set; } = new Dictionary<ElementId, bool>();

        public SelectionWindow(Document doc)
        {
            InitializeComponent();
            _doc = doc;
            Loaded += SelectionWindow_Loaded;
        }

        private void SelectionWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var views = new FilteredElementCollector(_doc)
                            .OfClass(typeof(ViewPlan))
                            .Cast<ViewPlan>()
                            .Where(v => v.ViewType == ViewType.FloorPlan && !v.IsTemplate)
                            .OrderBy(v => v.Name);

            foreach (var view in views)
            {
                CheckBox viewCheckbox = new CheckBox
                {
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Tag = view.Id
                };

                TextBlock viewNameText = new TextBlock
                {
                    Text = view.Name,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 5, 0)
                };

                bool hasStairs = new FilteredElementCollector(_doc, view.Id)
                                    .OfCategory(BuiltInCategory.OST_Stairs)
                                    .WhereElementIsNotElementType()
                                    .Any();

                StackPanel panel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center
                };

                panel.Children.Add(viewCheckbox);
                panel.Children.Add(viewNameText);

                if (hasStairs)
                {
                    CheckBox stairsCheckbox = new CheckBox
                    {
                        Content = "Stairs",
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(10, 0, 0, 0)
                    };
                    panel.Children.Add(stairsCheckbox);
                }

                ViewsListBox.Items.Add(panel);
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            SelectedCategories.Clear();
            SelectedViewIds.Clear();
            SelectedViewStairs.Clear();

            foreach (var item in ViewsListBox.Items)
            {
                var panel = item as StackPanel;
                if (panel == null)
                    continue;

                var cbView = panel.Children.OfType<CheckBox>().FirstOrDefault();
                var cbStairs = panel.Children.OfType<CheckBox>().Skip(1).FirstOrDefault();

                if (cbView != null && cbView.IsChecked == true)
                {
                    if (cbView.Tag is ElementId id)
                    {
                        SelectedViewIds.Add(id);
                        bool stairsChecked = cbStairs != null && cbStairs.IsChecked == true;
                        SelectedViewStairs[id] = stairsChecked;

                        if (stairsChecked)
                            SelectedCategories.Add((int)BuiltInCategory.OST_Stairs);
                    }
                }
            }

            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        // No longer needed (unless you want to keep global checkbox logic)
        public bool IsStairsChecked { get; private set; }
    }
}







