import os
from PIL import Image

def add_padding_to_images(folder_path='sprites', padding=64):
    """
    Adds transparent padding to all PNG images in a specified folder without resizing.

    Args:
        folder_path (str): The path to the folder containing the images.
        padding (int): The number of transparent pixels to add to each side.
    """
    if not os.path.isdir(folder_path):
        print(f"Error: Folder '{folder_path}' not found.")
        return

    print(f"Adding padding to PNG files in '{folder_path}'...")

    for filename in os.listdir(folder_path):
        if filename.lower().endswith('.png'):
            try:
                filepath = os.path.join(folder_path, filename)
                img = Image.open(filepath)

                # Ensure image is in RGBA to handle transparency
                if img.mode != 'RGBA':
                    img = img.convert('RGBA')

                # Calculate the new size with padding
                new_width = img.width + 2 * padding
                new_height = img.height + 2 * padding

                # Create a new image with a transparent background
                padded_img = Image.new('RGBA', (new_width, new_height), (0, 0, 0, 0))

                # Paste the original image onto the center of the new image
                padded_img.paste(img, (padding, padding))

                # Save the padded image, replacing the original file
                padded_img.save(filepath, 'PNG')

                print(f"Processed: {filename} -> New dimensions: {new_width}x{new_height}")

            except Exception as e:
                print(f"Error processing {filename}: {e}")

    print("Processing complete. ✨")

if __name__ == '__main__':
    # Ensure you have a backup of your original images before running.
    add_padding_to_images()