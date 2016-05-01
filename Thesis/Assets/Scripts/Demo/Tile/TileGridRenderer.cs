using UnityEngine;
using System.Collections.Generic;
using Grammars.Tiles;
using Grammars;
using System;
using System.Collections;
using Grammars.Events;

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
        Grammar<TileGrid> grammar = null;
        public int gridWidth = 70;
        public int gridHeight = 70;

        public IElementRenderer CurrentElement {
            get {
                return currentTile;
            }
        }

        public StructureModel Source {
            get {
                if (grid == null) {
                    grid = new TileGrid(gridWidth, gridHeight);
                }
                return grid;
            }
        }

        public IGrammarEventHandler Grammar {
            get {
                return grammar;
            }

            set {
                if (value == null) {
                    grammar = null;
                } else if (value.GetType() == typeof(Grammar<TileGrid>)) {
                    grammar = (Grammar<TileGrid>)value;
                } else {
                    print("Wrong grammar type for this demo renderer.");
                }
            }
        }

        public string Name {
            get {
                if (grammar != null) {
                    return grammar.Name;
                } else return "";
            }
        }

        // Use this for initialization
        void Start() {
            if (controller.currentStructureRenderer == null) controller.RegisterStructureRenderer(this);
            if (gridLineObject == null) {
                gridLineObject = new GameObject("Gridlines");
                gridLineObject.transform.SetParent(transform);
                gridLineObject.transform.localPosition = new Vector3(0, 0, -2);
            }
            gridLines = new List<LineRenderer>();
            InitGridLines();

            // Create the graph
            if(grid == null) grid = new TileGrid(gridWidth, gridHeight);
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
                if ((object)controller.currentStructureRenderer == this) {
                    cameraControl.cameraPanBlocked = controller.paused;
                    if (Input.GetKeyDown(KeyCode.K) && !controller.paused) {
                        if (gridLineObject != null) {
                            gridLineObject.SetActive(!gridLineObject.activeSelf);
                        }
                    }
                }

                // Add tiles
                if ((object)controller.currentStructureRenderer == this && Input.GetMouseButtonDown(1) && !controller.paused) {
                    if (currentTile == null) {
                        Vector3 newPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                        Vector3 tilePos = RendererToTilePosition(newPos.x, newPos.y);
                        int x = (int)Math.Round(tilePos.x, 0);
                        int y = (int)Math.Round(tilePos.y, 0);
                        if (x < 0 || x >= grid.GetGridSize().x) return;
                        if (y < 0 || y >= grid.GetGridSize().y) return;
                        Tile tile = new Tile(grid, x, y);
                        if (controller.defaultClass != null) {
                            tile.AddAttributeClass(controller.defaultClass);
                        }
                    }
                }

                // Remove tiles
                if ((object)controller.currentStructureRenderer == this && Input.GetKey(KeyCode.X) && !controller.paused) {
                    if (currentTile != null) {
                        currentTile.GetTile().Destroy();
                    }
                }

                if ((object)controller.currentStructureRenderer == this) {
                    int w = grid.GetGridSize().x;
                    int h = grid.GetGridSize().y;
                    if (Input.GetKeyDown(KeyCode.Keypad6)) {
                        grid.SetGridSize(w + 1, h, 0, 0);
                    } else if (Input.GetKeyDown(KeyCode.Keypad4)) {
                        grid.SetGridSize(w - 1, h, 0, 0);
                    } else if (Input.GetKeyDown(KeyCode.Keypad8)) {
                        grid.SetGridSize(w, h + 1, 0, 0);
                    } else if (Input.GetKeyDown(KeyCode.Keypad2)) {
                        grid.SetGridSize(w, h - 1, 0, 0);
                    }
                }

                /*if (!controller.paused) {
                    if (Input.GetKeyDown(KeyCode.F)) {
                        StartCoroutine("FindTransform");
                    }
                }*/
            }
		}

        public void AddTileRenderer(int x, int y) {
            Tile tile = grid.GetTile(x, y);
            if (tile == null) return;
            TileRenderer obj = new GameObject().AddComponent<TileRenderer>();
            obj.transform.SetParent(transform);
            obj.gameObject.name = "Tile X" + x + " Y" + y;
            obj.gridRenderer = this;
            obj.gameObject.transform.localPosition = TileToRendererPosition(x, y);
            obj.SetTile(tile);
            if (tileRenderers == null) SyncGridSize();
            tileRenderers[x, y] = obj;
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

        public void SetGrid(TileGrid grid) {
            if (grid == null) return;
            if (this.grid != null) {
                this.grid.StructureChanged -= new EventHandler(TileGridStructureChanged);
            }
            this.grid = grid;
            grid.StructureChanged += new EventHandler(TileGridStructureChanged);
            updateRenderer = true;
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
                    line.SetPosition(0, gridLineObject.transform.position + TileToRendererPosition(x, 0) - new Vector3(30, 30));
                    line.SetPosition(1, gridLineObject.transform.position + TileToRendererPosition(x, h) - new Vector3(30, 30));
                }
                for (int y = 0; y <= h; y++) {
                    LineRenderer line = new GameObject("Line").AddComponent<LineRenderer>();
                    InitLineRenderer(line);
                    gridLines.Add(line);
                    line.SetPosition(0, gridLineObject.transform.position + TileToRendererPosition(0, y) - new Vector3(30, 30));
                    line.SetPosition(1, gridLineObject.transform.position + TileToRendererPosition(w, y) - new Vector3(30, 30));
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
                        if(tileRen != null) Destroy(tileRen.gameObject);
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

        public IEnumerator SaveStructure() {
            string dateTime = DateTime.Now.Day.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Year.ToString()
                + "_" + DateTime.Now.Hour.ToString() + "-" + DateTime.Now.Minute.ToString() + "-" + DateTime.Now.Second.ToString()
                + "_" + DateTime.Now.Millisecond.ToString();
            string filename = "Grammars/Grid_" + dateTime + ".xml";
            DemoIO serializer = new DemoIO(filename, controller);
            serializer.SerializeGrid(grid);
            print("Saved!");
            yield return null;
        }

        public IEnumerator LoadStructure() {
            string filename = "Grammars/Grid_test.xml";
            DemoIO serializer = new DemoIO(filename, controller);
            TileGrid newGrid = serializer.DeserializeGrid();
            SetGrid(newGrid);
            print("Loaded!");
            yield return null;
        }

        public IEnumerator GrammarStep() {
            if (grammar != null) {
                grammar.Update();
            }
            yield return null;
        }
    }
}