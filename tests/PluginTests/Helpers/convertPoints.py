import json
import re

def convert_wallpoints(wallpoints_path, config_path):
    # read JSON files
    with open(wallpoints_path, 'r') as f:
        wallpoints_data = json.load(f)

    with open(config_path, 'r') as f:
        config_data = json.load(f)

    crop_regions = config_data["cropRegions"]

    # Create a dictionary for quick access by floorPlanId (viewNameID)
    crop_region_map = {
        region["viewNameID"]: region
        for region in crop_regions
    }

    for point in wallpoints_data["wallPoints"]:
        floor_plan_id = point["location"]["floorPlanId"]
        coord = point["location"]["coord"]

        if floor_plan_id not in crop_region_map:
            print(f"⚠️  FloorPlan ID '{floor_plan_id}' not found in config.")
            continue

        region = crop_region_map[floor_plan_id]

        minX = region["min"]["x"]
        maxX = region["max"]["x"]
        minY = region["min"]["y"]
        maxY = region["max"]["y"]
        imageWidth = region["width"]
        imageHeight = region["height"]

        # Conversion functions
        convertX = lambda x: (x - minX) / (maxX - minX) * imageWidth
        convertY = lambda y: (maxY - y) / (maxY - minY) * imageHeight

        old_x, old_y = coord["x"], coord["y"]
        coord["x"] = convertX(old_x)
        coord["y"] = convertY(old_y)

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

    # Overwrite the original file
    with open(wallpoints_path, 'w') as f:
        f.write(json_text)

    print(f"✅ Converted coordinates saved to: {wallpoints_path}")


convert_wallpoints("C:./TestFiles/WallPoints/wallPoints_4.json", "./TestFiles/Models/model_4.json")

