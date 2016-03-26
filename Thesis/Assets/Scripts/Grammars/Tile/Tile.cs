using System;
using System.Collections.Generic;
using Util;

namespace Grammars.Tile {
    public class Tile : AttributedElement {
        TileGrid grid;
		int x, y;
        bool destroyed = false;

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

        public Pair GetIndices() {
            return new Pair(x, y);
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
                    case "_structure_neighbors":
                        result = GetNeighbors().Count.ToString(); break;
                    case "_structure_neighbour_up":
                        Tile u = grid.GetTile(x, y - 1);
                        result = (u != null).ToString(); break;
                    case "_structure_neighbour_down":
                        Tile d = grid.GetTile(x, y + 1);
                        result = (d != null).ToString(); break;
                    case "_structure_neighbour_left":
                        Tile l = grid.GetTile(x - 1, y);
                        result = (l != null).ToString(); break;
                    case "_structure_neighbour_right":
                        Tile r = grid.GetTile(x + 1, y);
                        result = (r != null).ToString(); break;
                }
            }
            return result;
        }
    }
}
