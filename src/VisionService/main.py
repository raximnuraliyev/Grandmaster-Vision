"""
Grandmaster Vision - Chess Board Recognition Service
Converts chess board images to FEN notation using computer vision.
"""

from fastapi import FastAPI, UploadFile, File, HTTPException
from fastapi.middleware.cors import CORSMiddleware
import cv2
import numpy as np
from PIL import Image
import io
import chess
from typing import Optional
import logging

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = FastAPI(
    title="Grandmaster Vision - Image Recognition",
    description="Chess board image to FEN converter",
    version="1.0.0"
)

# CORS for Blazor frontend
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Piece detection mapping
PIECE_CLASSES = {
    0: 'P', 1: 'N', 2: 'B', 3: 'R', 4: 'Q', 5: 'K',  # White pieces
    6: 'p', 7: 'n', 8: 'b', 9: 'r', 10: 'q', 11: 'k',  # Black pieces
    12: None  # Empty square
}


class ChessBoardDetector:
    """Detects chess board and pieces from image."""

    def __init__(self, model_path: Optional[str] = None):
        self.model = None
        self.model_path = model_path
        # Will load YOLO model when available

    def preprocess_image(self, image: np.ndarray) -> np.ndarray:
        """Preprocess image for board detection."""
        # Convert to grayscale
        gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)

        # Apply adaptive thresholding
        thresh = cv2.adaptiveThreshold(
            gray, 255, cv2.ADAPTIVE_THRESH_GAUSSIAN_C,
            cv2.THRESH_BINARY, 11, 2
        )
        return thresh

    def detect_board_corners(self, image: np.ndarray) -> Optional[np.ndarray]:
        """Detect the four corners of the chess board using Hough lines."""
        gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
        edges = cv2.Canny(gray, 50, 150, apertureSize=3)

        # Detect lines
        lines = cv2.HoughLinesP(
            edges, 1, np.pi/180, threshold=100,
            minLineLength=100, maxLineGap=10
        )

        if lines is None:
            return None

        # Find horizontal and vertical lines
        horizontal = []
        vertical = []

        for line in lines:
            x1, y1, x2, y2 = line[0]
            angle = np.arctan2(y2 - y1, x2 - x1) * 180 / np.pi

            if abs(angle) < 10 or abs(angle) > 170:
                horizontal.append(line[0])
            elif abs(abs(angle) - 90) < 10:
                vertical.append(line[0])

        # Return approximate corners based on line intersections
        if len(horizontal) >= 2 and len(vertical) >= 2:
            # Get bounding box of detected lines
            all_points = np.vstack([horizontal, vertical])
            x_coords = np.concatenate([all_points[:, 0], all_points[:, 2]])
            y_coords = np.concatenate([all_points[:, 1], all_points[:, 3]])

            return np.array([
                [x_coords.min(), y_coords.min()],
                [x_coords.max(), y_coords.min()],
                [x_coords.max(), y_coords.max()],
                [x_coords.min(), y_coords.max()]
            ])

        return None

    def extract_squares(self, image: np.ndarray, board_region: Optional[np.ndarray] = None) -> list:
        """Extract 64 squares from the board image."""
        if board_region is None:
            # Assume full image is the board
            h, w = image.shape[:2]
            board_region = np.array([[0, 0], [w, 0], [w, h], [0, h]])

        # Get the bounding rectangle
        x_min, y_min = board_region[:, 0].min(), board_region[:, 1].min()
        x_max, y_max = board_region[:, 0].max(), board_region[:, 1].max()

        board_img = image[int(y_min):int(y_max), int(x_min):int(x_max)]
        h, w = board_img.shape[:2]

        square_h = h // 8
        square_w = w // 8

        squares = []
        for row in range(8):
            for col in range(8):
                y1, y2 = row * square_h, (row + 1) * square_h
                x1, x2 = col * square_w, (col + 1) * square_w
                square = board_img[y1:y2, x1:x2]
                squares.append(square)

        return squares

    def classify_square(self, square: np.ndarray) -> Optional[str]:
        """
        Classify a single square to determine piece type.
        Uses color histogram analysis as fallback when model not loaded.
        """
        if self.model is not None:
            # Use YOLO model for classification
            # results = self.model.predict(square)
            # return PIECE_CLASSES.get(results[0].class_id)
            pass

        # Fallback: Basic color analysis
        # This is a simplified heuristic - real implementation needs trained model
        gray = cv2.cvtColor(square, cv2.COLOR_BGR2GRAY)
        mean_val = np.mean(gray)
        std_val = np.std(gray)

        # Empty squares have low variance
        if std_val < 15:
            return None

        # Analyze piece color based on central region
        h, w = square.shape[:2]
        center = square[h//4:3*h//4, w//4:3*w//4]
        center_gray = cv2.cvtColor(center, cv2.COLOR_BGR2GRAY)
        center_mean = np.mean(center_gray)

        # This is a placeholder - needs actual piece recognition
        return None

    def image_to_fen(self, image: np.ndarray) -> str:
        """Convert chess board image to FEN notation."""
        # Detect board corners
        corners = self.detect_board_corners(image)

        # Extract squares
        squares = self.extract_squares(image, corners)

        # Classify each square
        board = []
        for i, square in enumerate(squares):
            piece = self.classify_square(square)
            board.append(piece)

        # Convert to FEN
        fen_rows = []
        for row in range(8):
            fen_row = ""
            empty_count = 0

            for col in range(8):
                piece = board[row * 8 + col]
                if piece is None:
                    empty_count += 1
                else:
                    if empty_count > 0:
                        fen_row += str(empty_count)
                        empty_count = 0
                    fen_row += piece

            if empty_count > 0:
                fen_row += str(empty_count)
            fen_rows.append(fen_row)

        # Return FEN (position only, default other fields)
        position_fen = "/".join(fen_rows)
        return f"{position_fen} w KQkq - 0 1"


# Initialize detector
detector = ChessBoardDetector()


@app.get("/")
async def root():
    """Health check endpoint."""
    return {"status": "healthy", "service": "Grandmaster Vision"}


@app.post("/api/recognize")
async def recognize_board(file: UploadFile = File(...)):
    """
    Recognize chess board from uploaded image.

    Returns FEN notation of the detected position.
    """
    try:
        # Read image
        contents = await file.read()
        nparr = np.frombuffer(contents, np.uint8)
        image = cv2.imdecode(nparr, cv2.IMREAD_COLOR)

        if image is None:
            raise HTTPException(status_code=400, detail="Invalid image file")

        logger.info(f"Processing image: {file.filename}, size: {image.shape}")

        # Convert to FEN
        fen = detector.image_to_fen(image)

        # Validate FEN using python-chess
        try:
            board = chess.Board(fen)
            is_valid = board.is_valid()
        except ValueError:
            is_valid = False

        return {
            "fen": fen,
            "valid": is_valid,
            "image_size": {"width": image.shape[1], "height": image.shape[0]}
        }

    except Exception as e:
        logger.error(f"Error processing image: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))


@app.post("/api/validate-fen")
async def validate_fen(fen: str):
    """Validate a FEN string."""
    try:
        board = chess.Board(fen)
        return {
            "valid": board.is_valid(),
            "fen": board.fen(),
            "board_visual": str(board)
        }
    except ValueError as e:
        return {"valid": False, "error": str(e)}


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
