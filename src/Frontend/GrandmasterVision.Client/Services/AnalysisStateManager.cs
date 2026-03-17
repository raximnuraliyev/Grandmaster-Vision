using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GrandmasterVision.Client.Services
{
    /// <summary>
    /// Centralized state management for the chess analysis UI.
    /// Decouples component state from business logic.
    /// </summary>
    public class AnalysisStateManager
    {
        // Observable events for reactive updates
        public event Action? OnStateChanged;
        public event Action<ArrowVisualizationState>? OnArrowsChanged;
        public event Action<EngineEvaluation>? OnEvaluationChanged;
        public event Action<int>? OnMoveSelected;

        // Current State
        private AnalysisState _state = new();
        private EngineEvaluation _evaluation = new();
        private ArrowVisualizationState _arrows = new();
        private SettingsState _settings = new();

        public AnalysisState State => _state;
        public EngineEvaluation Evaluation => _evaluation;
        public ArrowVisualizationState Arrows => _arrows;
        public SettingsState Settings => _settings;

        /// <summary>
        /// Update the current position
        /// </summary>
        public void SetPosition(string fen, int moveNumber = 0)
        {
            _state.CurrentFen = fen;
            _state.CurrentMoveIndex = moveNumber;
            _state.LastUpdated = DateTime.UtcNow;
            OnStateChanged?.Invoke();
        }

        /// <summary>
        /// Update engine evaluation
        /// </summary>
        public void SetEvaluation(double? centipawns, int? mateIn, int depth, string bestMove, string pv = "")
        {
            _evaluation = new EngineEvaluation
            {
                Centipawns = centipawns,
                MateIn = mateIn,
                Depth = depth,
                BestMove = bestMove,
                PrincipalVariation = pv,
                Timestamp = DateTime.UtcNow
            };

            // Auto-update arrows with best move
            if (!string.IsNullOrEmpty(bestMove) && bestMove.Length >= 4)
            {
                var from = bestMove.Substring(0, 2);
                var to = bestMove.Substring(2, 2);
                SetBestMoveArrow(from, to);
            }

            OnEvaluationChanged?.Invoke(_evaluation);
            OnStateChanged?.Invoke();
        }

        /// <summary>
        /// Set arrow visualization mode
        /// </summary>
        public void SetArrowMode(ArrowMode mode)
        {
            _settings.ArrowMode = mode;
            _arrows.Mode = mode;
            OnArrowsChanged?.Invoke(_arrows);
            OnStateChanged?.Invoke();
        }

        /// <summary>
        /// Toggle threat arrow visibility
        /// </summary>
        public void SetShowThreatArrows(bool show)
        {
            _settings.ShowThreatArrows = show;
            _arrows.ShowThreatArrows = show;
            OnArrowsChanged?.Invoke(_arrows);
            OnStateChanged?.Invoke();
        }

        /// <summary>
        /// Set the best move arrow
        /// </summary>
        public void SetBestMoveArrow(string fromSquare, string toSquare)
        {
            _arrows.BestMoveArrow = new ArrowData
            {
                FromSquare = fromSquare,
                ToSquare = toSquare,
                Type = ArrowType.BestMove,
                Timestamp = DateTime.UtcNow
            };
            OnArrowsChanged?.Invoke(_arrows);
        }

        /// <summary>
        /// Add a threat arrow
        /// </summary>
        public void AddThreatArrow(string fromSquare, string toSquare)
        {
            _arrows.ThreatArrows.Add(new ArrowData
            {
                FromSquare = fromSquare,
                ToSquare = toSquare,
                Type = ArrowType.Threat,
                Timestamp = DateTime.UtcNow
            });
            OnArrowsChanged?.Invoke(_arrows);
        }

        /// <summary>
        /// Add alternative line arrows
        /// </summary>
        public void AddAlternativeArrow(string fromSquare, string toSquare)
        {
            _arrows.AlternativeArrows.Add(new ArrowData
            {
                FromSquare = fromSquare,
                ToSquare = toSquare,
                Type = ArrowType.Alternative,
                Timestamp = DateTime.UtcNow
            });
            OnArrowsChanged?.Invoke(_arrows);
        }

        /// <summary>
        /// Clear all arrows
        /// </summary>
        public void ClearArrows()
        {
            _arrows.BestMoveArrow = null;
            _arrows.ThreatArrows.Clear();
            _arrows.AlternativeArrows.Clear();
            OnArrowsChanged?.Invoke(_arrows);
        }

        /// <summary>
        /// Update the move list
        /// </summary>
        public void SetMoveHistory(List<MoveRecord> moves)
        {
            _state.MoveHistory = moves;
            OnStateChanged?.Invoke();
        }

        /// <summary>
        /// Add a move to history
        /// </summary>
        public void AddMove(MoveRecord move)
        {
            _state.MoveHistory.Add(move);
            _state.CurrentMoveIndex = _state.MoveHistory.Count - 1;
            OnStateChanged?.Invoke();
        }

        /// <summary>
        /// Navigate to a specific move
        /// </summary>
        public void GoToMove(int index)
        {
            if (index >= 0 && index < _state.MoveHistory.Count)
            {
                _state.CurrentMoveIndex = index;
                _state.CurrentFen = _state.MoveHistory[index].Fen;
                OnMoveSelected?.Invoke(index);
                OnStateChanged?.Invoke();
            }
        }

        /// <summary>
        /// Set player information
        /// </summary>
        public void SetPlayers(PlayerInfo white, PlayerInfo black)
        {
            _state.WhitePlayer = white;
            _state.BlackPlayer = black;
            OnStateChanged?.Invoke();
        }

        /// <summary>
        /// Set opening information
        /// </summary>
        public void SetOpening(string eco, string name)
        {
            _state.OpeningEco = eco;
            _state.OpeningName = name;
            OnStateChanged?.Invoke();
        }

        /// <summary>
        /// Toggle board flip
        /// </summary>
        public void ToggleFlip()
        {
            _state.IsFlipped = !_state.IsFlipped;
            OnStateChanged?.Invoke();
        }

        /// <summary>
        /// Set analyzing state
        /// </summary>
        public void SetAnalyzing(bool analyzing)
        {
            _state.IsAnalyzing = analyzing;
            OnStateChanged?.Invoke();
        }

        /// <summary>
        /// Update settings
        /// </summary>
        public void UpdateSettings(SettingsState settings)
        {
            _settings = settings;
            _arrows.Mode = settings.ArrowMode;
            _arrows.ShowThreatArrows = settings.ShowThreatArrows;
            OnArrowsChanged?.Invoke(_arrows);
            OnStateChanged?.Invoke();
        }

        /// <summary>
        /// Reset all state
        /// </summary>
        public void Reset()
        {
            _state = new AnalysisState();
            _evaluation = new EngineEvaluation();
            _arrows = new ArrowVisualizationState();
            OnStateChanged?.Invoke();
        }
    }

    /// <summary>
    /// Main analysis state container
    /// </summary>
    public class AnalysisState
    {
        public string CurrentFen { get; set; } = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        public int CurrentMoveIndex { get; set; } = -1;
        public List<MoveRecord> MoveHistory { get; set; } = new();
        public bool IsFlipped { get; set; } = false;
        public bool IsAnalyzing { get; set; } = false;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // Player Info
        public PlayerInfo WhitePlayer { get; set; } = new() { Name = "White", Rating = "?" };
        public PlayerInfo BlackPlayer { get; set; } = new() { Name = "Black", Rating = "?" };

        // Opening
        public string? OpeningEco { get; set; }
        public string? OpeningName { get; set; }

        // Last move highlights
        public string? LastMoveFrom { get; set; }
        public string? LastMoveTo { get; set; }
    }

    /// <summary>
    /// Engine evaluation state
    /// </summary>
    public class EngineEvaluation
    {
        public double? Centipawns { get; set; }
        public int? MateIn { get; set; }
        public int Depth { get; set; }
        public string BestMove { get; set; } = "";
        public string PrincipalVariation { get; set; } = "";
        public List<EngineLine> TopLines { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public string FormatEval()
        {
            if (MateIn.HasValue)
                return MateIn > 0 ? $"+M{MateIn}" : $"-M{Math.Abs(MateIn.Value)}";
            if (!Centipawns.HasValue)
                return "0.00";
            var sign = Centipawns >= 0 ? "+" : "";
            return $"{sign}{Centipawns:F2}";
        }

        public bool IsPositive => MateIn > 0 || (Centipawns ?? 0) > 0;
        public bool IsNegative => MateIn < 0 || (Centipawns ?? 0) < 0;
        public bool IsMate => MateIn.HasValue;
    }

    /// <summary>
    /// Engine line data
    /// </summary>
    public class EngineLine
    {
        public int Rank { get; set; }
        public double? Evaluation { get; set; }
        public int? MateIn { get; set; }
        public string FirstMove { get; set; } = "";
        public string PrincipalVariation { get; set; } = "";

        public string FormatEval()
        {
            if (MateIn.HasValue)
                return MateIn > 0 ? $"+M{MateIn}" : $"-M{Math.Abs(MateIn.Value)}";
            if (!Evaluation.HasValue)
                return "0.00";
            var sign = Evaluation >= 0 ? "+" : "";
            return $"{sign}{Evaluation:F2}";
        }
    }

    /// <summary>
    /// Arrow visualization state
    /// </summary>
    public class ArrowVisualizationState
    {
        public ArrowMode Mode { get; set; } = ArrowMode.BestMove;
        public bool ShowThreatArrows { get; set; } = false;
        public ArrowData? BestMoveArrow { get; set; }
        public List<ArrowData> ThreatArrows { get; set; } = new();
        public List<ArrowData> AlternativeArrows { get; set; } = new();
        public List<ArrowData> CoachArrows { get; set; } = new();

        public List<ArrowData> GetVisibleArrows()
        {
            var arrows = new List<ArrowData>();

            if (Mode == ArrowMode.BestMove || Mode == ArrowMode.Both)
            {
                if (BestMoveArrow != null)
                    arrows.Add(BestMoveArrow);
            }

            if (Mode == ArrowMode.Coach || Mode == ArrowMode.Both)
            {
                arrows.AddRange(CoachArrows);
            }

            if (ShowThreatArrows)
            {
                arrows.AddRange(ThreatArrows);
            }

            arrows.AddRange(AlternativeArrows);

            return arrows;
        }
    }

    /// <summary>
    /// Individual arrow data
    /// </summary>
    public class ArrowData
    {
        public string FromSquare { get; set; } = "";
        public string ToSquare { get; set; } = "";
        public ArrowType Type { get; set; } = ArrowType.BestMove;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public string Color => Type switch
        {
            ArrowType.BestMove => "#4a9eff",
            ArrowType.Threat => "#ff5555",
            ArrowType.Coach => "#ffd700",
            ArrowType.Alternative => "#9b59b6",
            _ => "#ffffff"
        };

        public string TypeString => Type switch
        {
            ArrowType.BestMove => "best-move",
            ArrowType.Threat => "threat",
            ArrowType.Coach => "coach",
            ArrowType.Alternative => "alternative",
            _ => "best-move"
        };
    }

    /// <summary>
    /// Move record for the ledger
    /// </summary>
    public class MoveRecord
    {
        public int Number { get; set; }
        public bool IsWhiteMove { get; set; }
        public string San { get; set; } = "";
        public string Uci { get; set; } = "";
        public string Fen { get; set; } = "";
        public double? Evaluation { get; set; }
        public int? MateIn { get; set; }
        public MoveClassification Classification { get; set; } = MoveClassification.None;
    }

    /// <summary>
    /// Player information
    /// </summary>
    public class PlayerInfo
    {
        public string Name { get; set; } = "";
        public string Rating { get; set; } = "";
        public string? AvatarUrl { get; set; }
        public int MaterialAdvantage { get; set; } = 0;
        public List<char> CapturedPieces { get; set; } = new();
        public TimeSpan? TimeRemaining { get; set; }
        public bool IsActive { get; set; } = false;
    }

    /// <summary>
    /// Settings state
    /// </summary>
    public class SettingsState
    {
        public ArrowMode ArrowMode { get; set; } = ArrowMode.BestMove;
        public bool ShowThreatArrows { get; set; } = false;
        public bool ShowMoveClassification { get; set; } = true;
        public bool AutoplayMoves { get; set; } = false;
        public int AutoplayDelayMs { get; set; } = 1500;
        public bool HighlightLastMove { get; set; } = true;
        public bool ShowCoordinates { get; set; } = true;
        public bool SoundsEnabled { get; set; } = true;
    }

    /// <summary>
    /// Arrow visualization mode
    /// </summary>
    public enum ArrowMode
    {
        BestMove,
        Coach,
        Both,
        None
    }

    /// <summary>
    /// Arrow type
    /// </summary>
    public enum ArrowType
    {
        BestMove,
        Threat,
        Coach,
        Alternative
    }

    /// <summary>
    /// Move classification
    /// </summary>
    public enum MoveClassification
    {
        None,
        Brilliant,
        Great,
        Best,
        Excellent,
        Good,
        Book,
        Inaccuracy,
        Mistake,
        Blunder,
        Miss
    }
}
