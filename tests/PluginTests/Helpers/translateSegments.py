import json
import re

def convert_segments_with_coordinates(segments_path, points_path, output_path):
    # load data
    with open(segments_path, 'r') as f:
        segments_data = json.load(f)

    with open(points_path, 'r') as f:
        points_data = json.load(f)

    # Create a dictionary to lookup coordinates by ID
    point_lookup = {
        point["id"]: point["location"]["coord"]
        for point in points_data["wallPoints"]
    }

    new_segments = []

    for segment in segments_data["wallSegments"]:
        wall_point_ids = segment["wallPoints"]

        try:
            point_coords = [point_lookup[pid] for pid in wall_point_ids]
        except KeyError as e:
            print(f"Error: point not found for ID: {e}")
            continue

        point_coords_clean = [{"x": coord["x"], "y": coord["y"]} for coord in point_coords]

        new_segment = {
            "wallPoints": point_coords_clean,
            "wallTypeId": segment["wallTypeId"],
            "originType": segment["originType"],
            "status": segment["status"]
        }

        new_segments.append(new_segment)

    output_data = {"wallSegments": new_segments}

    # Serialize as an indented string
    json_string = json.dumps(output_data, indent=2)

    # Replace each multiline list of points with a single-line version
    # Use a regular expression to find blocks of "wallPoints"
    def compact_wallpoints(match):
        text = match.group()
        # Extract the list and load it as JSON
        points = json.loads("{" + text + "}")
        # Serialize the list back as a single line
        compact = json.dumps(points["wallPoints"], separators=(',', ': '))
        return f'"wallPoints": {compact}'

    compacted_json_string = re.sub(
        r'"wallPoints": \[\s*{[^]]+?}\s*]',  # Search for lists with dictionaries
        compact_wallpoints,
        json_string,
        flags=re.DOTALL
    )

    # Save the final file
    with open(output_path, 'w') as f:
        f.write(compacted_json_string)

    print(f"✅ File saved at: {output_path}")


convert_segments_with_coordinates(
    segments_path='./bin/Debug/net9.0/build_files/tempFolder/Template/wallSegments.json',
    points_path='./bin/Debug/net9.0/build_files/tempFolder/wallPoints_reverted.json',
    output_path='./bin/Debug/net9.0/build_files/tempFolder/wallSegments_coords.json'
)
