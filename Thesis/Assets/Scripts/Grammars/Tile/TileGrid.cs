using System;
using Util;

namespace Grammars.Tile {
    public class TileGrid : StructureModel {
        Tile[,] grid;

		public TileGrid(int width, int height) : base() {
            grid = new Tile[width, height];
		}

        public Pair GetGridSize() {
            return new Pair(grid.GetLength(0), grid.GetLength(1));
        }

        public bool SetGridSize(int width, int height, int xOffset, int yOffset) {
            if (grid != null && (width != grid.GetLength(0) || height != grid.GetLength(1))) {
                Tile[,] gridCopy = grid;
                grid = new Tile[width, height];
                // Just set new indices for tiles that are still accessible, destroy the rest.
                for (int x = 0; x < gridCopy.GetLength(0); x++) {
                    int newX = x + xOffset;
                    for (int y = 0; y < gridCopy.GetLength(1); y++) {
                        int newY = y + yOffset;
                        if (newX < 0 || newX >= width || newY < 0 || newY >= height) {
                            if (gridCopy[x, y] != null) gridCopy[x, y].Destroy(true);
                        } else {
                            if(gridCopy[x, y] != null) gridCopy[x, y].SetIndices(newX, newY);
                            grid[newX, newY] = gridCopy[x, y];
                        }
                    }
                }
                OnStructureChanged(EventArgs.Empty);
                return true;
            } else return false;
        }

        public void CopyGrid(TileGrid source, int xOffset, int yOffset) {
            if (grid != null) {
                int w = Math.Min(grid.GetLength(0), source.GetGridSize().x-xOffset);
                int h = Math.Min(grid.GetLength(1), source.GetGridSize().y-yOffset);
                for (int x = 0; x < w; x++) {
                    for (int y = 0; y < h; y++) {
                        Tile tile = new Tile(this, x, y);
                        tile.SetAttributesUsingDifference(null, source.GetTile(xOffset + x, yOffset + y)); // copy attributes
                    }
                }
                OnStructureChanged(EventArgs.Empty);
            }
        }

        public TileGrid GetView(int x, int y, int w, int h) {
            TileGrid view = new TileGrid(w, h);
            view.CopyGrid(this, x, y);
            return view;
        }

        // ************************** GRID MANAGEMENT ************************** \\
        // The following code only adds and removes tiles to/from the grid
        // Element creation should be handled outside of this class.
        public bool SetTile(int x, int y, Tile tile) {
            if (x < 0 || x >= grid.GetLength(0)) return false;
            if (y < 0 || y >= grid.GetLength(1)) return false;
            if (grid[x, y] != null && !grid[x, y].Equals(tile)) {
                grid[x, y].Destroy(true);
            }
            grid[x, y] = tile;
            OnStructureChanged(EventArgs.Empty);
            return true;
        }

        public Tile GetTile(int x, int y) {
            if (x < 0 || x >= grid.GetLength(0)) return null;
            if (y < 0 || y >= grid.GetLength(1)) return null;
            return grid[x, y];
        }
    }
}
