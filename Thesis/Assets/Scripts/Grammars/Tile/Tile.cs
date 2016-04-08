﻿using System.Collections.Generic;

namespace Grammars.Tile {
    public class Tile : AttributedElement {
        TileGrid grid;
		int x, y;
        bool destroyed = false;

        public override IElementContainer Container {
            get {
                return grid;
            }
        }

        public Tile(TileGrid grid, int x, int y) : base() {
			this.grid = grid;
            this.x = x;
            this.y = y;
			grid.SetTile(x, y, this);
		}

        public void SetIndices(int x, int y) {
            this.x = x;
            this.y = y;
        }

        public TilePos GetIndices() {
            return new TilePos(x, y);
        }

		public IDictionary<string, Tile> GetNeighbors() {
			Dictionary<string, Tile> neighbors = new Dictionary<string, Tile>();
            Tile l = grid.GetTile(x - 1, y);
            Tile r = grid.GetTile(x + 1, y);
            Tile u = grid.GetTile(x, y - 1);
            Tile d = grid.GetTile(x, y + 1);
            if (l != null) neighbors.Add("l", l);
            if (r != null) neighbors.Add("r", r);
            if (u != null) neighbors.Add("u", u);
            if (d != null) neighbors.Add("d", d);
            return neighbors;
		}

        public bool IsAdjacent(Tile t) {
            Tile l = grid.GetTile(x - 1, y); if (t == l) return true;
            Tile r = grid.GetTile(x + 1, y); if (t == r) return true;
            Tile u = grid.GetTile(x, y - 1); if (t == u) return true;
            Tile d = grid.GetTile(x, y + 1); if (t == d) return true;
            return false;
        }

		public void Destroy(bool gridReplaced = false) {
            if (!destroyed) {
                destroyed = true;
                // This method should contain any additional deconstruction stuff if necessary
                if(!gridReplaced) grid.SetTile(x, y, null);
            }
		}

        public override string GetAttribute(string key, bool raw = false) {
            string result = base.GetAttribute(key, raw);
            if (result == null && key != null && key.StartsWith("_structure_")) {
                switch (key) {
                    case "_structure_type":
                        result = "tile"; break;
                    case "_structure_x":
                        result = x.ToString(); break;
                    case "_structure_y":
                        result = y.ToString(); break;
                    case "_structure_neighbors_count":
                        result = GetNeighbors().Count.ToString(); break;
                    case "_structure_has_neighbour_up":
                        Tile u = grid.GetTile(x, y - 1);
                        result = (u != null).ToString(); break;
                    case "_structure_has_neighbour_down":
                        Tile d = grid.GetTile(x, y + 1);
                        result = (d != null).ToString(); break;
                    case "_structure_has_neighbour_left":
                        Tile l = grid.GetTile(x - 1, y);
                        result = (l != null).ToString(); break;
                    case "_structure_has_neighbour_right":
                        Tile r = grid.GetTile(x + 1, y);
                        result = (r != null).ToString(); break;
                }
            }
            return result;
        }

        public override object GetObjectAttribute(string key) {
            object result = base.GetObjectAttribute(key);
            if (result == null && key != null && key.StartsWith("_structure_")) {
                switch (key) {
                    case "_structure_neighbour_up":
                        Tile u = grid.GetTile(x, y - 1);
                        result = u; break;
                    case "_structure_neighbour_down":
                        Tile d = grid.GetTile(x, y + 1);
                        result = d; break;
                    case "_structure_neighbour_left":
                        Tile l = grid.GetTile(x - 1, y);
                        result = l; break;
                    case "_structure_neighbour_right":
                        Tile r = grid.GetTile(x + 1, y);
                        result = r; break;
                }
            }
            return result;
        }
    }
}
