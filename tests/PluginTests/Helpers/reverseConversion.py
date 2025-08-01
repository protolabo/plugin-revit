import json
import re

def revert_wallpoints(wallpoints_path, image_data_path, output_path):
    # read JSON files
    with open(wallpoints_path, 'r') as f:
        wallpoints_data = json.load(f)

    with open(image_data_path, 'r') as f:
        image_data_array = json.load(f)

    # Create a dictionary for quick access by floorPlanId
    crop_region_map = {
        region["floorPlanId"]: region
        for region in image_data_array
    }

    for point in wallpoints_data["wallPoints"]:
        floor_plan_id = point["location"]["floorPlanId"]
        coord = point["location"]["coord"]

        if floor_plan_id not in crop_region_map:
            print(f"⚠️  FloorPlan ID '{floor_plan_id}' no encontrado en imageData.")
            continue

        region = crop_region_map[floor_plan_id]

        minX = region["min"]["x"]
        maxX = region["max"]["x"]
        minY = region["min"]["y"]
        maxY = region["max"]["y"]
        imageWidth = region["width"]
        imageHeight = region["height"]

        # Inverse conversion functions
        invertX = lambda x: x / imageWidth * (maxX - minX) + minX
        invertY = lambda y: maxY - (y / imageHeight * (maxY - minY))

        old_x, old_y = coord["x"], coord["y"]
        coord["x"] = invertX(old_x)
        coord["y"] = invertY(old_y)

    # Pretty-printed dump
    json_text = json.dumps(wallpoints_data, indent=2)

    # Compact 'coord' objects into a single line
    pattern = re.compile(
        r'("coord": )\{\n\s+"x": ([0-9.\-e]+),\n\s+"y": ([0-9.\-e]+)\n\s+\}',
        re.MULTILINE
    )

    def replacer(match):
        return f'{match.group(1)}{{ "x": {match.group(2)}, "y": {match.group(3)} }}'

    json_text = pattern.sub(replacer, json_text)

    # Save to a new file
    with open(output_path, 'w') as f:
        f.write(json_text)

    print(f"✅ Coordenadas revertidas y guardadas en: {output_path}")


revert_wallpoints(
    "./bin/Debug/net9.0/build_files/tempFolder/Template/wallPoints.json", 
    "./bin/Debug/net9.0/build_files/tempFolder/imageData.json", 
    "./bin/Debug/net9.0/build_files/tempFolder/wallPoints_reverted.json"
)

