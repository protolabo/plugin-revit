# This script prepares the C# source files in the "ExportClasses" folder by:
# - Deleting all files in the folder except "ModelData.cs"
# - Copying a predefined list of source files from a source directory
# - Commenting out all lines that include Revit-specific namespaces (e.g., Autodesk.Revit.DB and UI)
# - Replacing 'return Result.Succeeded;' with 'return true;', and 'Result.Failed' with 'false'
# - Commenting out lines that display TaskDialogs
# - Changing method signatures that return 'Result' to return 'bool' instead
# This is useful to adapt Revit-dependent code to be testable in a standalone environment.

import os
import shutil
import sys
import re
sys.stdout.reconfigure(encoding='utf-8')

# Target directory
export_dir = './ExportClasses'

# Delete all files in ExportClasses except ModelData.cs
for file in os.listdir(export_dir):
    full_path = os.path.join(export_dir, file)
    if file != 'ModelInfo.cs' and os.path.isfile(full_path):
        os.remove(full_path)
        print(f'🗑️ Deleted: {file}')

# Files to copy and modify
files = [
    "AttenuationUpdater.cs",
    "Getters.cs",
    "ImageJsonFileCreator.cs",
    "ModelData.cs",
    "PointAndSegment.cs",
    "SegmentsListCreator.cs",
    "WallsInserter.cs",
    "WallSplitter.cs",
    "ModelData.cs",
    "IDGenerator.cs"
]

for filename in files:
    source = f'C:/Users/pelon/source/repos/Create/Create/ExportClasses/{filename}'
    destination = os.path.join(export_dir, filename)

    shutil.copy2(source, destination)
    print(f'📁 File copied from {source} to {destination}')

    # Target phrases and replacements
    target_using_1 = 'using Autodesk.Revit.DB'
    target_using_2 = 'using Autodesk.Revit.UI'
    target_return_success = 'return Result.Succeeded;'
    target_return_failed = 'return Result.Failed;'
    replacement_success = 'return true;'
    replacement_failed = 'return false;'

    # Flags to track changes
    found_using_1 = False
    found_using_2 = False
    replaced_success = False
    replaced_failed = False

    filename_only = os.path.basename(destination)

    with open(destination, 'r', encoding='utf-8') as f:
        lines = f.readlines()

    new_lines = []

    for line in lines:
        stripped = line.lstrip()
        indent = line[:len(line) - len(stripped)]

        # Comment all "using Autodesk.Revit.DB"
        if target_using_1 in line and not stripped.startswith('//'):
            new_lines.append(f'{indent}// {stripped}')
            print(f'📝 Commented in {filename_only}: {line.strip()}')

        # Comment all "using Autodesk.Revit.UI"
        elif target_using_2 in line and not stripped.startswith('//'):
            new_lines.append(f'{indent}// {stripped}')
            print(f'📝 Commented in {filename_only}: {line.strip()}')

        # Replace all "return Result.Succeeded;" with "return true;"
        elif target_return_success in line and not stripped.startswith('//'):
            new_lines.append(f'{indent}// {stripped}')
            new_lines.append(f'{indent}{replacement_success}\n')
            print(f'🔁 Replaced return in {filename_only}: {line.strip()}')

        # Replace all "return Result.Failed;" with "return false;"
        elif target_return_failed in line and not stripped.startswith('//'):
            new_lines.append(f'{indent}// {stripped}')
            new_lines.append(f'{indent}{replacement_failed}\n')
            print(f'🔁 Replaced return in {filename_only}: {line.strip()}')

        # Comment all TaskDialog lines
        elif stripped.startswith('TaskDialog') and not stripped.startswith('//'):
            new_lines.append(f'{indent}// {stripped}')
            print(f'💬 Commented TaskDialog in {filename_only}: {line.strip()}')

        # Replace function signatures returning Result → bool
        elif re.match(r'^\s*public\s+static\s+Result\s+\w+\s*\(', line) and not stripped.startswith('//'):
            new_lines.append(f'{indent}// {stripped}')
            modified_line = re.sub(r'\bResult\b', 'bool', line, count=1)
            new_lines.append(f'{modified_line}')
            print(f'🔧 Changed function return type to bool in {filename_only}: {line.strip()}')

        # Replace private static → public static
        elif re.match(r'^\s*private\s+static\s+\w+', line) and not stripped.startswith('//'):
            new_lines.append(f'{indent}// {stripped}')
            modified_line = line.replace('private', 'public', 1)
            new_lines.append(f'{modified_line}')
            print(f'🔧 Changed function visibility to public in {filename_only}: {line.strip()}')

        else:
            new_lines.append(line)


    with open(destination, 'w', encoding='utf-8') as f_out:
        f_out.writelines(new_lines)

    # Report any targets not found
    if not found_using_1:
        print(f'⚠️  Line with "{target_using_1}" not found or already commented.')
    if not found_using_2:
        print(f'⚠️  Line with "{target_using_2}" not found or already commented.')
    if not replaced_success:
        print(f'⚠️  Line with "{target_return_success}" not found or already modified.')
    if not replaced_failed:
        print(f'⚠️  Line with "{target_return_failed}" not found or already modified.')
