# Grandmaster Vision

AI-Powered Chess Analysis Platform that converts visual or text-based chess data (Images/PGNs) into actionable, high-level analysis.

## Features

- **Image-to-Board Conversion**: Upload chess board screenshots and get instant FEN notation
- **Real-time Engine Analysis**: Powered by Stockfish 17 with 3000+ Elo strength
- **PGN Analysis**: Upload games and identify mistakes, blunders, and brilliant moves
- **Opening Recognition**: Automatic ECO code identification via Lichess API
- **Educational Insights**: Coach-style explanations for each position
- **Multi-platform**: Blazor WebAssembly frontend with C# backend

## Architecture

```
GrandmasterVision/
├── src/
│   ├── Backend/
│   │   └── GrandmasterVision.Api/     # ASP.NET Core 10 Web API
│   ├── Frontend/
│   │   └── GrandmasterVision.Client/  # Blazor WebAssembly
│   ├── GrandmasterVision.Core/        # Shared chess services
│   └── VisionService/                 # Python FastAPI for image recognition
├── engine/
│   └── stockfish/                     # Stockfish 17 engine
├── models/                            # ML models for piece detection
└── data/                              # Sample PGN files and images
```

## Prerequisites

- Docker and Docker Compose (recommended)
- OR: .NET 9 SDK + Python 3.10+

## Quick Start (Docker)

```bash
cd GrandmasterVision
docker-compose up --build
```

Open **http://localhost** - done!

### Services

| Service | Port | Description |
|---------|------|-------------|
| Frontend | 80 | Blazor WASM UI |
| API | 5000 | ASP.NET Core backend |
| Vision | 8000 | Python image recognition |

### Stop

```bash
docker-compose down
```

## Local Development (Without Docker)

### PowerShell (3 terminals)

**Terminal 1 - Vision Service:**
```powershell
cd src\VisionService
python -m venv venv
.\venv\Scripts\Activate.ps1
pip install -r requirements.txt
python main.py
```

**Terminal 2 - Backend API:**
```powershell
cd src\Backend\GrandmasterVision.Api
dotnet run
```

**Terminal 3 - Frontend:**
```powershell
cd src\Frontend\GrandmasterVision.Client
dotnet run
```

Open https://localhost:5002

## API Endpoints

### Analysis Controller

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/analysis/best-move` | Get best move for a FEN position |
| POST | `/api/analysis/top-moves` | Get top N moves with evaluations |
| POST | `/api/analysis/evaluate-move` | Evaluate a specific move |
| POST | `/api/analysis/analyze-pgn` | Full game analysis |
| POST | `/api/analysis/identify-opening` | Identify opening from FEN |
| POST | `/api/analysis/validate-fen` | Validate FEN string |

### Vision Controller

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/vision/recognize` | Convert board image to FEN |

## Performance Targets

- **Engine Strength**: 3000+ Elo (Stockfish 17)
- **Image Recognition**: < 2 seconds
- **Analysis Depth**: 20+ plies in < 5 seconds

## Technology Stack

| Component | Technology |
|-----------|------------|
| Frontend | Blazor WebAssembly (.NET 10) |
| Backend | ASP.NET Core 10 Web API |
| Chess Engine | Stockfish 17 (native + WASM) |
| Vision | Python FastAPI + OpenCV + YOLO |
| Database | PostgreSQL |

## License

MIT License
