using System;
using System.Collections.Generic;
using System.Linq;
using Util;

namespace Grammars.Tile {
	public class TileGridTransformer : IStructureTransformer<TileGrid> {
        TileGrid source = null;
        Pair selectedOffset;
        TileGrid query = null;
        bool findFirst = false;

        public TileGrid Source {
            get {
                return source;
            }
            set {
                if (source != value) {
                    selectedOffset = null;
                    query = null;
                    source = value;
                }
            }
        }

        public TileGridTransformer(bool findFirst = false) {
            selectedOffset = null;
            this.findFirst = findFirst;
		}

		public Pair GetSelectedOffset() {
            return selectedOffset;
		}

        public bool Find(TileGrid query) {
            if (source == null || query == null) return false;
            if (query.GetGridSize().x > source.GetGridSize().x || query.GetGridSize().y > source.GetGridSize().y) return false;
            this.query = query;

            // Sort tile indices in query on most specific.
            List<Pair> queryTiles = new List<Pair>();
            for (int x = 0; x < query.GetGridSize().x; x++) {
                for (int y = 0; y < query.GetGridSize().y; y++) {
                    queryTiles.Add(new Pair(x, y));
                }
            }
            queryTiles = queryTiles.OrderByDescending(t => (query.GetTile(t.x, t.y) == null ? 0 : query.GetTile(t.x, t.y).GetAttributes().Count)).ToList();

            List<Pair> matches = new List<Pair>();

            for (int sX = 0; sX < source.GetGridSize().x-query.GetGridSize().x+1; sX++) {
                for (int sY = 0; sY < source.GetGridSize().y-query.GetGridSize().y+1; sY++) {
                    bool matched = true;
                    foreach (Pair queryPos in queryTiles) {
                        Tile queryTile = query.GetTile(queryPos.x, queryPos.y);
                        Tile sourceTile = source.GetTile(sX + queryPos.x, sY + queryPos.y);
                        if (queryTile != null && sourceTile == null) {
                            matched = false;
                            break;
                        } else if (sourceTile != null) {
                            if (sourceTile.HasAttribute("_grammar_transformer_id") || (queryTile != null && !sourceTile.MatchAttributes(queryTile))) {
                                matched = false;
                                break;
                            }
                        }
                    }
                    if (matched) {
                        Pair match = new Pair(sX, sY);
                        if (findFirst) {
                            selectedOffset = match;
                            return true;
                        } else matches.Add(match);
                    }
                }
            }

            if (matches.Count > 0) {
                // Select one from the list randomly
                Random rnd = new Random();
                int r = rnd.Next(matches.Count);
                selectedOffset = matches.ElementAt(r);
                // Mark it so that it can't be accessed by other transformers
                /*for (int x = 0; x < query.GetGridSize().x; x++) {
                    for (int y = 0; y < query.GetGridSize().y; y++) {
                        Tile sourceTile = source.GetTile(selectedOffset.x + x, selectedOffset.y + y);
                        if(sourceTile != null) sourceTile.SetAttribute("_grammar_transformer_id", GetHashCode().ToString());
                    }
                }*/
                return true;
            } else return false;
        }

        public void Transform(TileGrid target) {
            if (source == null || query == null || target == null || selectedOffset == null) return;
            if (!target.GetGridSize().Equals(query.GetGridSize())) return;

            int w = query.GetGridSize().x;
            int h = query.GetGridSize().y;

            int sX = selectedOffset.x;
            int sY = selectedOffset.y;

            for (int x = 0; x < w; x++) {
                for (int y = 0; y < h; y++) {
                    Tile sourceTile = source.GetTile(sX + x, sY + y);
                    Tile queryTile = query.GetTile(x, y);
                    Tile targetTile = target.GetTile(x, y);

                    if(sourceTile != null) sourceTile.PostponeAttributeChanged(true);
                    if (targetTile != null) {
                        if (sourceTile == null) sourceTile = new Tile(source, sX + x, sY + y);
                        sourceTile.SetAttributesUsingDifference(queryTile, targetTile);
                        sourceTile.RemoveAttribute("_grammar_transformer_id");
                    } else if(queryTile != null) { // i.e. if tile was explicitly deleted during transition from query --> target
                        source.SetTile(sX + x, sY + y, null);
                    }
                    if (sourceTile != null) sourceTile.PostponeAttributeChanged(false);
                }
            }

            //selectedOffset = null;
            //query = null;
        }

        public void Destroy() {
            Source = null;
        }
    }
}
