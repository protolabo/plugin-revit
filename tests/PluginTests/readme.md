# Plugin Testing

This folder contains the testing environment for the Revit - Ekahau Plugin, which helps ensure its correct operation. This environment requires .NET to run. The tests are run in standalone mode and therefore do not require Revit to be running.

These tests include unit tests and integration tests.

This is the structure of the working environment.

```
    PLUGINTESTS/
    ├── ExportClasses/
    ├── Helpers/
    ├── TestClasses/
    ├── TestFiles/
    │   ├── Integration/
    │   │   ├── Models/
    │   │   ├── WallPoints/
    │   │   ├── Walls/
    │   │   └── WallSegments/
    │   ├── UnitTests/
    │   │   ├── ImageDatas/
    │   │   ├── WallDatas/
    │   │   └── WallList/
    ├── IntegrationTests.cs
    ├── UnitTests.cs
    ├── Program.cs
    ├── PluginTests.csproj
    ├── PluginTests.sln
    ├── test_report.txt
    └── readme.md
```

`ExportClasses` contains the .cs files where the methods to be tested are defined. <br>
`Helpers` contains auxiliary code in Python. <br>
`TestClasses`  contains the .cs files that evaluate the results of the classes contained in ExportClasses. <br>
`TestFiles` contains the files necessary for running the tests. <br>
`Models` contains a series of JSON files that contain the basic configuration of a simuilated Revit model; these files serve as the starting point for the integration tests. <br>
`WallPoints` contains the corresponding JSON files with the list of points for each model in Models/. <br>
`Walls` contains the JSON files that define the list of elements (walls, doors, windows, etc.) corresponding to each model in Models/. <br>
`WallSegments` contains the corresponding JSON files with the list of segmemnts for each model in Models/. <br>
`ImageDatas` contains JSON files with fictional information about the metadata of the exported images used as backgrounds in the Ekahau files. <br>
`WallDatas` contains fictional configuration files that map Revit walls to Ekahau walls. <br>
`WallList` contains the list of elements from a simulated Revit model in .txt files using the ShortHand format to simplify test definitions. <br>
`IntegrationTests.cs` performs integration tests for each file in Models/. <br>
`UnitTests.cs` performs the unit tests. <br>
`Program.cs` is the main class of the test project. <br>
`test_report.txt` contains the test results. <br>

## Unit Tests

The unit tests perform three tests: wall division, setting the scale of the Ekahau model, and the method responsible for obtaining the corresponding Ekahau ID for each Revit wall.

### Wall Division Tets

The `WallSplitterUnitTester.cs` class tests the `WallSplitter.SplitWallByOpening()` method as follows:
- Loads the elements from UnitTests/WallList
- Converts the data from the Shorthand format to the format accepted by the wall-splitting method
- Compares the results with the expected data

### Setting the scale Tets

The `GetMetersPerUnitTester.cs` class tests the `ImageJsonFileCreator.GetMetersPerUnit()` method as follows:
- Loads the elements from UnitTests/ImageDatas
- CCalculates the scale of the Ekahau file using the metadata from the fictional images.
- Compares the results with the expected data

### Getting Ekahau ID Tests

The `GettersTester.cs` class tests the methods in `Getters.cs` as follows:
- Loads data from  UnitTests/WallDatas
- Gets the corresponding Ekahau Wall for every Revit wall
- Gets corresponding Ekahau ID for teh walls.
- Calls the corresponding method within Getters.cs depending on the element type (wall, door, window) and compares the result with the expected ID.

## Integration Tests

Integration tests are carried out as follows:
- A JSON file from Models/ is taken, which contains the basic configuration of the simulation of a Revit model as well as the list of files corresponding to that model
- JSON files that make up an Ekahau (esx) file are generated from an empty template
- The generated files are compared with the test files

To verify that the JSON file generation was successful, among other things, it is checked that:
- the walls were correctly divided
- the number of generated points is the same as expected and their locations match within a small margin
- the number of generated segments is the same as expected and the points that make up each segment are the same as expected, within a small margin
- the update of attenuation values was carried out satisfactorily

## About the environment

In order to run the code that makes up the Plugin outside the Revit environment, before executing the tests, the script Helpers/prepareClasses.py is run automatically. This script creates a copy of the necessary .cs files for running the tests and performs the following changes:
- Comments out all Revit dependencies
- Changes variables of type Result to type bool
- Replaces `TaskDialog.Show` calls with `Console.WriteLine`
- Makes private classes public so they can be called individually

## Tets Results

The file test_report.txt contains the test results.

## Adding Tests

To add Unit Tests, the corresponding files within the folder that matches the type of unit test to which you want to add tests must be modified or added.

To add an integration test, the following files must be added:
- A model file in the Models/ folder
- A file with the list of elements in the Walls/ folder, as well as a file with the expected division of those elements in the same folder
- A file in the WallPoints/ folder with the expected list of points
- A file in the WallSegments/ folder with the expected list of segments