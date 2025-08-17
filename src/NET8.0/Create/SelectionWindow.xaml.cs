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

        public List<ElementId> SelectedViewIds { get; private set; } = new List<ElementId>();

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

                StackPanel panel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center
                };

                panel.Children.Add(viewCheckbox);
                panel.Children.Add(viewNameText);

                ViewsListBox.Items.Add(panel);
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            SelectedViewIds.Clear();

            foreach (var item in ViewsListBox.Items)
            {
                if (item is StackPanel panel)
                {
                    var cbView = panel.Children.OfType<CheckBox>().FirstOrDefault();

                    if (cbView != null && cbView.IsChecked == true)
                    {
                        if (cbView.Tag is ElementId id)
                        {
                            SelectedViewIds.Add(id);
                        }
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
    }
}








