/**
 * ArrowRenderer - High-performance SVG arrow drawing for chess analysis
 * Handles best move, threat, and coach visualization arrows
 */

class ArrowRenderer {
    constructor() {
        this.svg = null;
        this.arrows = new Map();
        this.squareSize = 60; // Will be recalculated
        this.boardSize = 480;
        this.flipped = false;
    }

    /**
     * Initialize the renderer with an SVG element
     * @param {string} svgId - ID of the SVG element
     * @param {number} boardSize - Size of the board in pixels
     */
    initialize(svgId, boardSize = 480) {
        this.svg = document.getElementById(svgId);
        if (!this.svg) {
            console.error(`ArrowRenderer: SVG element '${svgId}' not found`);
            return false;
        }
        this.boardSize = boardSize;
        this.squareSize = boardSize / 8;
        this.clearArrows();
        return true;
    }

    /**
     * Update board size (call on resize)
     * @param {number} newSize - New board size in pixels
     */
    updateBoardSize(newSize) {
        this.boardSize = newSize;
        this.squareSize = newSize / 8;
        // Redraw existing arrows
        const currentArrows = Array.from(this.arrows.values()).map(a => a.data);
        this.clearArrows();
        currentArrows.forEach(arrow => this.drawArrow(arrow));
    }

    /**
     * Set board flip state
     * @param {boolean} flipped - Whether the board is flipped
     */
    setFlipped(flipped) {
        if (this.flipped !== flipped) {
            this.flipped = flipped;
            // Redraw all arrows with new orientation
            const currentArrows = Array.from(this.arrows.values()).map(a => a.data);
            this.clearArrows();
            currentArrows.forEach(arrow => this.drawArrow(arrow));
        }
    }

    /**
     * Draw an arrow on the board
     * @param {Object} arrowData - Arrow configuration
     * @param {string} arrowData.from - Start square (e.g., "e2")
     * @param {string} arrowData.to - End square (e.g., "e4")
     * @param {string} arrowData.type - Arrow type: "best-move", "threat", "coach", "alternative"
     * @param {string} [arrowData.id] - Optional unique ID
     * @returns {string} Arrow ID
     */
    drawArrow(arrowData) {
        if (!this.svg) {
            console.error('ArrowRenderer: Not initialized');
            return null;
        }

        const { from, to, type = 'best-move', id } = arrowData;
        const arrowId = id || `arrow-${from}-${to}-${type}`;

        // Remove existing arrow with same ID
        this.removeArrow(arrowId);

        // Calculate coordinates
        const fromCoords = this.squareToPixel(from);
        const toCoords = this.squareToPixel(to);

        if (!fromCoords || !toCoords) {
            console.error(`ArrowRenderer: Invalid squares - from: ${from}, to: ${to}`);
            return null;
        }

        // Calculate line with offset to not cover piece centers completely
        const angle = Math.atan2(toCoords.y - fromCoords.y, toCoords.x - fromCoords.x);
        const offset = this.squareSize * 0.15;
        const arrowheadSize = this.getArrowheadSize(type);

        const startX = fromCoords.x + Math.cos(angle) * offset;
        const startY = fromCoords.y + Math.sin(angle) * offset;
        const endX = toCoords.x - Math.cos(angle) * (offset + arrowheadSize);
        const endY = toCoords.y - Math.sin(angle) * (offset + arrowheadSize);

        // Create SVG group
        const group = document.createElementNS('http://www.w3.org/2000/svg', 'g');
        group.setAttribute('id', arrowId);
        group.setAttribute('class', `arrow-group ${type}`);

        // Create line
        const line = document.createElementNS('http://www.w3.org/2000/svg', 'line');
        line.setAttribute('x1', startX);
        line.setAttribute('y1', startY);
        line.setAttribute('x2', endX);
        line.setAttribute('y2', endY);
        line.setAttribute('class', `arrow-line ${type}`);

        // Create arrowhead
        const arrowhead = this.createArrowhead(toCoords.x, toCoords.y, angle, type);

        group.appendChild(line);
        group.appendChild(arrowhead);
        this.svg.appendChild(group);

        // Store arrow data
        this.arrows.set(arrowId, {
            element: group,
            data: arrowData
        });

        return arrowId;
    }

    /**
     * Create an arrowhead polygon
     */
    createArrowhead(tipX, tipY, angle, type) {
        const size = this.getArrowheadSize(type);
        const spread = Math.PI / 5; // 36 degrees

        const point1 = { x: tipX, y: tipY };
        const point2 = {
            x: tipX - size * Math.cos(angle - spread),
            y: tipY - size * Math.sin(angle - spread)
        };
        const point3 = {
            x: tipX - size * Math.cos(angle + spread),
            y: tipY - size * Math.sin(angle + spread)
        };

        const polygon = document.createElementNS('http://www.w3.org/2000/svg', 'polygon');
        polygon.setAttribute('points', `${point1.x},${point1.y} ${point2.x},${point2.y} ${point3.x},${point3.y}`);
        polygon.setAttribute('class', `arrowhead ${type}`);

        return polygon;
    }

    /**
     * Get arrowhead size based on type
     */
    getArrowheadSize(type) {
        const baseSize = this.squareSize * 0.4;
        switch (type) {
            case 'best-move': return baseSize;
            case 'threat': return baseSize * 0.9;
            case 'coach': return baseSize * 0.8;
            case 'alternative': return baseSize * 0.75;
            default: return baseSize;
        }
    }

    /**
     * Convert square notation to pixel coordinates (center of square)
     * @param {string} square - Square notation (e.g., "e4")
     * @returns {{x: number, y: number}} Pixel coordinates
     */
    squareToPixel(square) {
        if (!square || square.length !== 2) return null;

        const file = square.charCodeAt(0) - 97; // 'a' = 0, 'h' = 7
        const rank = parseInt(square[1]) - 1;   // '1' = 0, '8' = 7

        if (file < 0 || file > 7 || rank < 0 || rank > 7) return null;

        let x, y;
        if (this.flipped) {
            x = (7 - file) * this.squareSize + this.squareSize / 2;
            y = rank * this.squareSize + this.squareSize / 2;
        } else {
            x = file * this.squareSize + this.squareSize / 2;
            y = (7 - rank) * this.squareSize + this.squareSize / 2;
        }

        return { x, y };
    }

    /**
     * Draw multiple arrows at once
     * @param {Array} arrows - Array of arrow data objects
     */
    drawArrows(arrows) {
        if (!Array.isArray(arrows)) return;
        arrows.forEach(arrow => this.drawArrow(arrow));
    }

    /**
     * Remove a specific arrow
     * @param {string} arrowId - ID of the arrow to remove
     */
    removeArrow(arrowId) {
        const arrow = this.arrows.get(arrowId);
        if (arrow && arrow.element) {
            arrow.element.remove();
            this.arrows.delete(arrowId);
        }
    }

    /**
     * Clear all arrows
     */
    clearArrows() {
        this.arrows.forEach(arrow => {
            if (arrow.element) {
                arrow.element.remove();
            }
        });
        this.arrows.clear();
    }

    /**
     * Clear arrows of a specific type
     * @param {string} type - Arrow type to clear
     */
    clearArrowsByType(type) {
        this.arrows.forEach((arrow, id) => {
            if (arrow.data.type === type) {
                arrow.element.remove();
                this.arrows.delete(id);
            }
        });
    }

    /**
     * Get count of current arrows
     */
    getArrowCount() {
        return this.arrows.size;
    }

    /**
     * Highlight a square (for showing valid moves, etc.)
     * @param {string} square - Square to highlight
     * @param {string} color - Highlight color
     */
    highlightSquare(square, color = 'rgba(255, 255, 0, 0.3)') {
        const coords = this.squareToPixel(square);
        if (!coords) return null;

        const id = `highlight-${square}`;
        this.removeArrow(id); // Use arrow system for cleanup

        const rect = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
        rect.setAttribute('x', coords.x - this.squareSize / 2);
        rect.setAttribute('y', coords.y - this.squareSize / 2);
        rect.setAttribute('width', this.squareSize);
        rect.setAttribute('height', this.squareSize);
        rect.setAttribute('fill', color);
        rect.setAttribute('id', id);
        rect.setAttribute('class', 'square-highlight');

        // Insert at beginning so arrows draw on top
        this.svg.insertBefore(rect, this.svg.firstChild);

        this.arrows.set(id, { element: rect, data: { type: 'highlight', square } });
        return id;
    }

    /**
     * Clear all highlights
     */
    clearHighlights() {
        this.arrows.forEach((arrow, id) => {
            if (arrow.data.type === 'highlight') {
                arrow.element.remove();
                this.arrows.delete(id);
            }
        });
    }
}

// Create global instance
window.arrowRenderer = new ArrowRenderer();

// Blazor JS Interop Functions
window.ArrowRenderer = {
    /**
     * Initialize the arrow renderer
     */
    initialize: function (svgId, boardSize) {
        return window.arrowRenderer.initialize(svgId, boardSize);
    },

    /**
     * Draw a single arrow
     */
    drawArrow: function (from, to, type, id) {
        return window.arrowRenderer.drawArrow({ from, to, type, id });
    },

    /**
     * Draw multiple arrows
     */
    drawArrows: function (arrowsJson) {
        const arrows = JSON.parse(arrowsJson);
        window.arrowRenderer.drawArrows(arrows);
    },

    /**
     * Clear all arrows
     */
    clearArrows: function () {
        window.arrowRenderer.clearArrows();
    },

    /**
     * Clear arrows by type
     */
    clearArrowsByType: function (type) {
        window.arrowRenderer.clearArrowsByType(type);
    },

    /**
     * Remove specific arrow
     */
    removeArrow: function (arrowId) {
        window.arrowRenderer.removeArrow(arrowId);
    },

    /**
     * Set board flip state
     */
    setFlipped: function (flipped) {
        window.arrowRenderer.setFlipped(flipped);
    },

    /**
     * Update board size
     */
    updateBoardSize: function (newSize) {
        window.arrowRenderer.updateBoardSize(newSize);
    },

    /**
     * Highlight a square
     */
    highlightSquare: function (square, color) {
        return window.arrowRenderer.highlightSquare(square, color);
    },

    /**
     * Clear all highlights
     */
    clearHighlights: function () {
        window.arrowRenderer.clearHighlights();
    }
};

console.log('ArrowRenderer loaded successfully');
