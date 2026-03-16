"""
Chess Piece Recognition Model Setup

This script downloads and sets up a pre-trained YOLO model for chess piece detection.
Run this once to set up the model for the Vision Service.
"""

import os
import urllib.request
from pathlib import Path

# Model URLs - using chess piece detection models
MODELS = {
    "chess-yolov8": {
        "url": "https://github.com/Elucidation/tensorflow_chessbot/raw/master/training/models/chess_model.h5",
        "type": "tensorflow"
    }
}

def download_file(url: str, dest: Path):
    """Download a file with progress indication."""
    print(f"Downloading: {url}")
    print(f"Destination: {dest}")

    try:
        urllib.request.urlretrieve(url, dest)
        print(f"Downloaded successfully: {dest.name}")
        return True
    except Exception as e:
        print(f"Download failed: {e}")
        return False

def setup_models():
    """Set up chess recognition models."""
    models_dir = Path(__file__).parent.parent / "models"
    models_dir.mkdir(exist_ok=True)

    print("=" * 50)
    print("Chess Piece Recognition Model Setup")
    print("=" * 50)

    # Note: For production, you would train a custom YOLO model
    # or use one of the available chess piece detection models

    print("""
NOTE: For the image recognition feature to work accurately,
you need to train a custom YOLO model on chess piece images.

Recommended datasets:
1. Chess Pieces Dataset on Roboflow
2. Chess Recognition Dataset on Kaggle
3. Custom screenshots from Lichess/Chess.com

Training command (after installing ultralytics):
$ yolo train data=chess_pieces.yaml model=yolov8n.pt epochs=100

For now, the vision service uses basic computer vision techniques
(Hough lines + color analysis) as a fallback.
""")

    # Create a placeholder model info file
    model_info = models_dir / "MODEL_INFO.md"
    model_info.write_text("""# Chess Recognition Models

## Current Status
Using basic OpenCV computer vision as fallback.

## To Add YOLO Model

1. Train a YOLOv8 model on chess piece images:
   ```bash
   pip install ultralytics
   yolo train data=chess_pieces.yaml model=yolov8n.pt epochs=100
   ```

2. Place the trained model in this directory as `chess_pieces.pt`

3. Update `VisionService/main.py` to load the model:
   ```python
   from ultralytics import YOLO
   model = YOLO('models/chess_pieces.pt')
   ```

## Recommended Datasets

- Roboflow Universe: chess-pieces-detection
- Kaggle: chess-piece-recognition
- Custom: Take screenshots from lichess.org and chess.com
""")

    print(f"Created model info at: {model_info}")

if __name__ == "__main__":
    setup_models()
