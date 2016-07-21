using System;
using System.Collections.Generic;
using System.Linq;

namespace Grammars.Tiles {
	public class TileGridTransformer : IStructureTransformer<TileGrid> {
        TileGrid source = null;
        TilePos selectedOffset;
        TileGrid query = null;
        bool findFirst = false;
        public bool FindFirst {
            get { return findFirst; }
            set { findFirst = value; }
        }

        private Rule<TileGrid> rule = null;
        public Rule<TileGrid> Rule {
            get { return rule; }
            set { rule = value; }
        }
        List<TilePos> matches;
        List<TilePos> prioritizedMatches = null;

        public TileGrid Source {
            get { return source; }
            set {
                if (source != value) {
                    if (matches != null) {
                        matches.Clear();
                        matches = null;
                    }
                    selectedOffset = null;
                    query = null;
                    source = value;
                }
            }
        }

        public IDictionary<string, AttributedElement> SelectedMatch {
            get {
                Dictionary<string, AttributedElement> dict = new Dictionary<string, AttributedElement>();
                if (source == null || query == null || selectedOffset == null) return dict;
                int w = query.GetGridSize().x;
                int h = query.GetGridSize().y;
                int sX = selectedOffset.x;
                int sY = selectedOffset.y;
                int rot = selectedOffset.Rotation;
                for (int x = 0; x < w; x++) {
                    for (int y = 0; y < h; y++) {
                        int curSX = sX + x; int curSY = sY + y;
                        switch (rot) {
                            case 0: curSX = sX + x; curSY = sY + y; break;
                            case 1: curSX = sX - y; curSY = sY + x; break;
                            case 2: curSX = sX - x; curSY = sY - y; break;
                            case 3: curSX = sX + y; curSY = sY - x; break;
                        }
                        Tile sourceTile = source.GetTile(curSX, curSY);
                        Tile queryTile = query == null ? null : query.GetTile(x, y);
                        if (queryTile != null) {
                            dict.Add(x + "_" + y, sourceTile);
                        }
                    }
                }
                return dict;
            }
        }

        protected Traverser<TileGrid> traverser = null;
        public Traverser<TileGrid> Traverser {
            get { return traverser; }
            set { traverser = value; }
        }

        public TileGridTransformer() {
            selectedOffset = null;
            findFirst = false;
            rule = null;
            traverser = null;
        }

		public TilePos GetSelectedOffset() {
            return selectedOffset;
		}

        public bool Find(TileGrid query) {
            prioritizedMatches = null;
            bool rotate = (query != null) && (query.GetAttribute("_grammar_rotate") != "false");
            if (query == null || query.GetElements().Count == 0) {
                if (source != null && source.GetElements().Count == 0) {
                    if (query != null) {
                        if ((query.GetGridSize().x > source.GetGridSize().x || query.GetGridSize().y > source.GetGridSize().y)) {
                            if (!rotate) return false;
                            if ((query.GetGridSize().x > source.GetGridSize().y || query.GetGridSize().y > source.GetGridSize().x)) return false;
                        }
                    }
                    this.query = query;
                    matches = new List<TilePos>();
                    selectedOffset = null;
                    return true;
                }
                return false;
            }

            if (source == null || query == null) return false;
            if ((query.GetGridSize().x > source.GetGridSize().x || query.GetGridSize().y > source.GetGridSize().y)) {
                if (!rotate) return false;
                if ((query.GetGridSize().x > source.GetGridSize().y || query.GetGridSize().y > source.GetGridSize().x)) return false;
            }
            this.query = query;

            // Sort tile indices in query on most specific.
            List<TilePos> queryTiles = new List<TilePos>();
            for (int x = 0; x < query.GetGridSize().x; x++) {
                for (int y = 0; y < query.GetGridSize().y; y++) {
                    queryTiles.Add(new TilePos(x, y));
                }
            }
            queryTiles = queryTiles.OrderByDescending(t => (query.GetTile(t.x, t.y) == null ? 0 : query.GetTile(t.x, t.y).GetAttributes().Count)).ToList();
            matches = new List<TilePos>();

            int maxRot = rotate ? 4 : 1;
            for (int curRot = 0; curRot < maxRot; curRot++) {
                if (curRot % 2 == 0) {
                    if ((query.GetGridSize().x > source.GetGridSize().x || query.GetGridSize().y > source.GetGridSize().y)) continue;
                } else {
                    if ((query.GetGridSize().x > source.GetGridSize().y || query.GetGridSize().y > source.GetGridSize().x)) continue;
                }
                int minSX = 0, minSY = 0, maxSX = 0, maxSY = 0;
                switch (curRot) {
                    case 0:
                        maxSX = source.GetGridSize().x - query.GetGridSize().x + 1;
                        maxSY = source.GetGridSize().y - query.GetGridSize().y + 1;
                        minSX = 0;
                        minSY = 0;
                        break;
                    case 1:
                        maxSX = source.GetGridSize().x;
                        maxSY = source.GetGridSize().y - query.GetGridSize().x + 1;
                        minSX = query.GetGridSize().y - 1;
                        minSY = 0;
                        break;
                    case 2:
                        maxSX = source.GetGridSize().x;
                        maxSY = source.GetGridSize().y;
                        minSX = query.GetGridSize().x - 1;
                        minSY = query.GetGridSize().y - 1;
                        break;
                    case 3:
                        maxSX = source.GetGridSize().x - query.GetGridSize().x + 1;
                        maxSY = source.GetGridSize().y;
                        minSX = 0;
                        minSY = query.GetGridSize().y - 1;
                        break;
                }
                for (int sX = minSX; sX < maxSX; sX++) {
                    for (int sY = minSY; sY < maxSY; sY++) {
                        bool prioritizeMatch = false;
                        bool matched = true;
                        bool traverserMatched = (Traverser != null && Traverser.CurrentElement != null) ? false : true;
                        foreach (TilePos queryPos in queryTiles) {
                            Tile queryTile = query.GetTile(queryPos.x, queryPos.y);
                            Tile sourceTile = null;
                            switch (curRot) {
                                case 0: sourceTile = source.GetTile(sX + queryPos.x, sY + queryPos.y); break;
                                case 1: sourceTile = source.GetTile(sX - queryPos.y, sY + queryPos.x); break;
                                case 2: sourceTile = source.GetTile(sX - queryPos.x, sY - queryPos.y); break;
                                case 3: sourceTile = source.GetTile(sX + queryPos.y, sY - queryPos.x); break;
                            }
                            if (queryTile != null && sourceTile == null) {
                                if (!queryTile.HasAttribute("_grammar_noTile")) {
                                    matched = false;
                                    break;
                                }
                            } else if (sourceTile != null) {
                                if (sourceTile.HasAttribute("_grammar_transformer_id")
                                    || (queryTile != null && queryTile.HasAttribute("_grammar_noTile"))
                                    || (queryTile != null && !MatchAttributes(sourceTile, queryTile))) {
                                    matched = false;
                                    break;
                                }
                                if (sourceTile.HasAttribute("_prioritizeMatch")) prioritizeMatch = true;
                            }
                            if (!traverserMatched && queryTile.HasAttribute("_grammar_current") && sourceTile == Traverser.CurrentElement) traverserMatched = true;
                        }
                        if (!traverserMatched) matched = false;
                        if (matched) {
                            TilePos match = new TilePos(sX, sY, rotation: curRot);
                            matches.Add(match);
                            if (prioritizeMatch) {
                                if (prioritizedMatches == null) prioritizedMatches = new List<TilePos>();
                                prioritizedMatches.Add(match);
                            }
                            if (findFirst) {
                                return true;
                            }
                        }
                    }
                }
            }

            if (matches.Count > 0) {
                return true;
            } else return false;
        }

        public void Select() {
            if (matches == null) return;
            if (matches.Count > 0) {
                if (prioritizedMatches != null && prioritizedMatches.Count > 0) matches = prioritizedMatches;
                int index = -1;
                if (rule != null && rule.MatchSelector != null) {
                    index = rule.MatchSelector.Select(matches);
                }
                if (index == -1) {
                    Random rnd = new Random();
                    index = rnd.Next(matches.Count);
                }
                selectedOffset = matches.ElementAt(index);
                // Mark it so that it can't be accessed by other transformers
                /*for (int x = 0; x < query.GetGridSize().x; x++) {
                    for (int y = 0; y < query.GetGridSize().y; y++) {
                        Tile sourceTile = source.GetTile(selectedOffset.x + x, selectedOffset.y + y);
                        if(sourceTile != null) sourceTile.SetAttribute("_grammar_transformer_id", GetHashCode().ToString());
                    }
                }*/
            } else {
                if ((query == null || query.GetElements().Count == 0) && (source != null && source.GetElements().Count == 0)) {
                    if (query != null && query.GetAttribute("_grammar_rotate") == "false") {
                        selectedOffset = new TilePos(0, 0);
                    } else {
                        // Select random rotation
                        Random r = new Random();
                        int rotation = r.Next(0, 4);
                        //UnityEngine.MonoBehaviour.print("Rotation: " + rotation);
                        int middleX, middleY;
                        int sGridX, sGridY;
                        sGridX = source.GetGridSize().x;
                        sGridY = source.GetGridSize().y;
                        //middleX = (int)Math.Round(sGridX / 4f) + r.Next(0,(int)Math.Round(sGridX / 2f));
                        //middleY = (int)Math.Round(sGridY / 4f) + r.Next(0, (int)Math.Round(sGridY / 2f));
                        middleX = (int)Math.Round(sGridX / 2f);
                        middleY = (int)Math.Round(sGridY / 2f);
                        /*int x = 0, y = 0;
                        switch (rotation) {
                            case 0: x = 0; y = 0; break;
                            case 1: x = source.GetGridSize().x - 1; y = 0; break;
                            case 2: x = source.GetGridSize().x - 1; y = source.GetGridSize().y - 1; break;
                            case 3: x = 0; y = source.GetGridSize().y - 1; break;
                        }*/
                        selectedOffset = new TilePos(middleX, middleY, rotation);
                    }
                } else selectedOffset = null;
            }
        }

        public void Transform(TileGrid target) {
            if (source == null || target == null || selectedOffset == null) return;
            if (query != null && (!target.GetGridSize().Equals(query.GetGridSize()))) return;

            int w = target.GetGridSize().x;
            int h = target.GetGridSize().y;

            int sX = selectedOffset.x;
            int sY = selectedOffset.y;
            int rot = selectedOffset.Rotation;
            
            if (query == null || (query != null && query.GetElements().Count == 0)) {
                // Position in the middle of the chosen position
                int hW = (int)Math.Round(w / 2f);
                int hH = (int)Math.Round(h / 2f);
                switch (rot) {
                    case 0: sX -= hW; sY -= hH; break;
                    case 1: sX += hH; sY -= hW; break;
                    case 2: sX += hW; sY += hH; break;
                    case 3: sX -= hH; sY += hW; break;
                }
            }
            for (int x = 0; x < w; x++) {
                for (int y = 0; y < h; y++) {
                    int curSX = sX + x; int curSY = sY + y;
                    switch (rot) {
                        case 0: curSX = sX + x; curSY = sY + y; break;
                        case 1: curSX = sX - y; curSY = sY + x; break;
                        case 2: curSX = sX - x; curSY = sY - y; break;
                        case 3: curSX = sX + y; curSY = sY - x; break;
                    }
                    Tile sourceTile = source.GetTile(curSX, curSY);
                    Tile queryTile = query == null ? null : query.GetTile(x, y);
                    Tile targetTile = target.GetTile(x, y);

                    if (queryTile != null && queryTile.HasAttribute("_grammar_noTile")) {
                        queryTile = null;
                    }

                    if(sourceTile != null) sourceTile.PostponeAttributeChanged(true);
                    if (targetTile != null) {
                        if (sourceTile == null) sourceTile = new Tile(source, curSX, curSY);
                        SetAttributesUsingDifference(sourceTile, queryTile, targetTile);
                        sourceTile.RemoveAttribute("_grammar_transformer_id");
                    } else if(queryTile != null) { // i.e. if tile was explicitly deleted during transition from query --> target
                        source.SetTile(curSX, curSY, null);
                    }
                    if (sourceTile != null) sourceTile.PostponeAttributeChanged(false);
                }
            }

            //selectedOffset = null;
            //query = null;
        }

        protected bool MatchAttributes(AttributedElement sourceEl, AttributedElement queryEl) {
            if (sourceEl == null || queryEl == null) return false;
            if (rule != null) {
                sourceEl.SetObjectAttribute("grammar", rule.Grammar, notify: false);
                sourceEl.SetObjectAttribute("rule", rule, notify: false);
                queryEl.SetObjectAttribute("_grammar_matching", sourceEl, notify: false);
                bool match = sourceEl.MatchAttributes(queryEl);
                queryEl.RemoveObjectAttribute("_grammar_matching", notify: false);
                sourceEl.RemoveObjectAttribute("grammar", notify: false);
                sourceEl.RemoveObjectAttribute("rule", notify: false);
                return match;
            } else return sourceEl.MatchAttributes(queryEl);
        }

        protected void SetAttributesUsingDifference(AttributedElement sourceEl, AttributedElement queryEl, AttributedElement targetEl) {
            if (sourceEl == null || targetEl == null) return;
            if (rule != null) {
                sourceEl.SetObjectAttribute("grammar", rule.Grammar, notify: false);
                sourceEl.SetObjectAttribute("rule", rule, notify: false);
                sourceEl.SetAttributesUsingDifference(queryEl, targetEl, notify: false);
                sourceEl.RemoveObjectAttribute("grammar", notify: false);
                sourceEl.RemoveObjectAttribute("rule", notify: false);
            } else sourceEl.SetAttributesUsingDifference(queryEl, targetEl, notify: false);
        }

        public void Destroy() {
            Source = null;
        }
    }
}
