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
	}
}
