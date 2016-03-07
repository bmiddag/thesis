using UnityEngine;
using System.Collections.Generic;
using Grammars.Tile;
using Grammars;
using System;
using System.Collections;

namespace Demo {
	public class TileGridRenderer : MonoBehaviour, IStructureRenderer {
        TileGrid grid;
        public TileRenderer currentTile = null;
        TileRenderer[,] tileRenderers = null;
        public GameObject gridLineObject;

        List<LineRenderer> gridLines;
        
        public CameraControl cameraControl;

        bool updateRenderer = false; // If true, tilerenderers will be updated during the next call of Update(). Prevents chaining of renderer updates.

        public DemoController controller;

        public IElementRenderer CurrentElement {
            get {
                return currentTile;
            }
        }

        IEnumerator FindTransform() {
            TileGrid query = new TileGrid(4, 3);
            TileGrid target = new TileGrid(4, 3);

            for (int x = 0; x < 4; x++) {
                for (int y = 0; y < 3; y++) {
                    if (x != 3) {
                        Tile tile = new Tile(query, x, y);
                        if (x == y) tile.SetAttribute("_demo_color", "blue");
                    }
                    if (x != 0) {
                        Tile tarTile = new Tile(target, x, y);
                        if(x-1 == y) tarTile.SetAttribute("_demo_color", "blue");
                    }
                }
            }

            TileGridTransformer t = new TileGridTransformer();
            t.Source = grid;
            bool found = t.Find(query);
            if (found) {
                print("Found it!");
                //print(t.);
            } else {
                print("Not found");
            }
            //yield return new WaitForSeconds(2);
            if(found) t.Transform(target);
            yield return null;
        }

        // Use this for initialization
        void Start() {
            controller.RegisterStructureRenderer(this);
            gridLines = new List<LineRenderer>();
            InitGridLines();

            // Create the graph
            grid = new TileGrid(30, 30);
            grid.StructureChanged += TileGridStructureChanged;
            updateRenderer = true;
        }

        // Update is called once per frame
        void Update() {
            if (grid != null) {
                if (updateRenderer) {
                    updateRenderer = false;
                    SyncTileStructure();                    
                }

                // Pan
                cameraControl.cameraPanBlocked = controller.paused;

                // Add tiles
                if (Input.GetMouseButtonDown(1) && !controller.paused) {
                    if (currentTile == null) {
                        Vector3 newPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                        Vector3 tilePos = RendererToTilePosition(newPos.x, newPos.y);
                        int x = (int)Math.Round(tilePos.x, 0);
                        int y = (int)Math.Round(tilePos.y, 0);
                        if (x < 0 || x >= grid.GetGridSize().x) return;
                        if (y < 0 || y >= grid.GetGridSize().y) return;
                        Tile tile = new Tile(grid, x, y);
                        //controller.AddAttributeClass(tile, "white_circles");
                    }
                }

                // Remove tiles
                if (Input.GetKey(KeyCode.X) && !controller.paused) {
                    if (currentTile != null) {
                        currentTile.GetTile().Destroy();
                    }
                }

                if (Input.GetKeyDown(KeyCode.F)) {
                    StartCoroutine("FindTransform");
                }
            }
		}

        public void AddTileRenderer(int x, int y) {
            Tile tile = grid.GetTile(x, y);
            if (tile == null) return;
            TileRenderer obj = new GameObject().AddComponent<TileRenderer>();
            obj.gameObject.name = "Tile X" + x + " Y" + y;
            obj.gridRenderer = this;
            obj.gameObject.transform.position = TileToRendererPosition(x, y);
            obj.SetTile(tile);
            if (tileRenderers == null) SyncGridSize();
            tileRenderers[x, y] = obj;
            obj.transform.SetParent(transform);
        }

        public Vector3 TileToRendererPosition(int x, int y) {
            return new Vector3(x * 60, y * 60);
        }

        public Vector3 RendererToTilePosition(float x, float y) {
            return new Vector3(x / 60f, y / 60f);
        }

        public void RemoveTileRenderer(int x, int y) {
            // Remove current node status
            TileRenderer tileToRemove = tileRenderers[x, y];
            if (tileToRemove == null) return;
            if (currentTile == tileToRemove) {
                currentTile = null;
            }
            tileRenderers[x, y] = null;
            Tile tile = tileToRemove.GetTile();
            Destroy(tileToRemove.gameObject);
            if (tile != null) {
                // Remove tile as well
                tileToRemove.GetTile().Destroy();
            }
        }

        LineRenderer InitLineRenderer(LineRenderer line) {
            line.SetWidth(3.0f, 3.0f);
            Material mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = Color.black;
            line.material = mat;
            line.gameObject.name = "Gridline";
            if (gridLineObject != null) {
                line.transform.SetParent(gridLineObject.transform);
            } else line.transform.SetParent(transform);
            return line;
        }

        // ************************** STRUCTURE UPDATES ************************** \\
        private void TileGridStructureChanged(object sender, EventArgs e) {
            updateRenderer = true; // SyncTileStructure will be called during next update.
        }

        void OnDestroy() {
            if (grid != null) {
                grid.StructureChanged -= new EventHandler(TileGridStructureChanged);
            }
        }

        void InitGridLines() {
            if (grid == null) return;
            if (gridLines != null) {
                foreach (LineRenderer line in gridLines) {
                    Destroy(line.gameObject);
                }
                gridLines.Clear();

                int w = grid.GetGridSize().x;
                int h = grid.GetGridSize().y;

                for (int x = 0; x <= w; x++) {
                    LineRenderer line = new GameObject("Line").AddComponent<LineRenderer>();
                    InitLineRenderer(line);
                    gridLines.Add(line);
                    line.SetPosition(0, TileToRendererPosition(x, 0) - new Vector3(30, 30));
                    line.SetPosition(1, TileToRendererPosition(x, h) - new Vector3(30, 30));
                }
                for (int y = 0; y <= h; y++) {
                    LineRenderer line = new GameObject("Line").AddComponent<LineRenderer>();
                    InitLineRenderer(line);
                    gridLines.Add(line);
                    line.SetPosition(0, TileToRendererPosition(0, y) - new Vector3(30, 30));
                    line.SetPosition(1, TileToRendererPosition(w, y) - new Vector3(30, 30));
                }
            }
        }

        void SyncGridSize() {
            if (grid == null) return;
            int w = grid.GetGridSize().x;
            int h = grid.GetGridSize().y;
            if (tileRenderers == null || tileRenderers.GetLength(0) != w || tileRenderers.GetLength(1) != h) {
                InitGridLines();
                if (tileRenderers != null) {
                    foreach (TileRenderer tileRen in tileRenderers) {
                        Destroy(tileRen.gameObject);
                    }
                }
                tileRenderers = new TileRenderer[w, h];
                for (int x = 0; x < w; x++) {
                    for (int y = 0; y < h; y++) {
                        if (grid.GetTile(x, y) == null) {
                            tileRenderers[x, y] = null;
                        } else {
                            AddTileRenderer(x, y);
                        }
                    }
                }
            }
        }

        void SyncTileStructure() {
            if (grid != null) {
                SyncGridSize();
                int w = grid.GetGridSize().x;
                int h = grid.GetGridSize().y;
                for (int x = 0; x < w; x++) {
                    for (int y = 0; y < h; y++) {
                        if (tileRenderers[x, y] != null && !tileRenderers[x, y].GetTile().Equals(grid.GetTile(x, y))) {
                            // Remove tile renderer
                            RemoveTileRenderer(x, y);
                        }
                        if (tileRenderers[x, y] == null && grid.GetTile(x, y) != null) {
                            // Add tile renderer
                            AddTileRenderer(x, y);
                        }
                    }
                }
            }
        }
    }
}