using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Collections.Generic;

namespace ExportToEkahau
{
    [Transaction(TransactionMode.Manual)]
    public class Class1 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {  
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;
            View view = doc.ActiveView;
            TaskDialog.Show("vue", view.Id.ToString());

            FilteredElementCollector wall_collector = new FilteredElementCollector(doc);
            FilteredElementCollector window_collector = new FilteredElementCollector(doc);
            FilteredElementCollector door_collector = new FilteredElementCollector(doc);
            FilteredElementCollector stairs_collector = new FilteredElementCollector(doc);
            FilteredElementCollector level_collector = new FilteredElementCollector(doc);
            FilteredElementCollector plan_collector = new FilteredElementCollector(doc);

            wall_collector.OfCategory(BuiltInCategory.OST_Walls);
            window_collector.OfCategory(BuiltInCategory.OST_Windows);
            door_collector.OfCategory(BuiltInCategory.OST_Doors);
            stairs_collector.OfCategory(BuiltInCategory.OST_Stairs);
            level_collector.OfCategory(BuiltInCategory.OST_Levels);
            plan_collector.OfCategory(BuiltInCategory.OST_Views);

            FilteredElementIterator wall_iterator = wall_collector.GetElementIterator();
            FilteredElementIterator window_iterator = window_collector.GetElementIterator();
            FilteredElementIterator door_iterator = door_collector.GetElementIterator();
            FilteredElementIterator stairs_iterator = stairs_collector.GetElementIterator();
            FilteredElementIterator level_iterator = level_collector.GetElementIterator();
            FilteredElementIterator plan_iterator = plan_collector.GetElementIterator();

            WallType? wall = wall_iterator.GetCurrent() as WallType;
            TaskDialog.Show("object type 1", wall.FamilyName + ", " + wall_collector.GetElementCount());
            FamilySymbol? window = window_iterator.GetCurrent() as FamilySymbol;
            TaskDialog.Show("object type 2", window.FamilyName + ", " + window_collector.GetElementCount());
            FamilySymbol? door = door_iterator.GetCurrent() as FamilySymbol;
            TaskDialog.Show("object type 3", door.FamilyName + ", " + door_collector.GetElementCount());
            ElementType? stairs = stairs_iterator.GetCurrent() as ElementType;
            TaskDialog.Show("object type 4", stairs.FamilyName + ", " + stairs_collector.GetElementCount());
            LevelType? level = level_iterator.GetCurrent() as LevelType;
            TaskDialog.Show("object type 5", level.Id + ", " + level.LevelId + ", " + level_collector.GetElementCount());
            ViewPlan? plan = plan_iterator.GetCurrent() as ViewPlan;
            TaskDialog.Show("object type 6", plan.ViewType + ", " + plan.Title + ", " + stairs_collector.GetElementCount());
            IList<ElementId> lvl_elements = plan.GetDependentElements(null);
            TaskDialog.Show("lvl_elements", lvl_elements.Count + "");
            IEnumerator< ElementId > lvl_elements_enumerator = lvl_elements.GetEnumerator();
            string print_value = "";
            while (lvl_elements_enumerator.MoveNext())
            {
                print_value += lvl_elements_enumerator.Current.GetType().ToString() + " ";
            }

            return Result.Succeeded;
        }
    }
}
