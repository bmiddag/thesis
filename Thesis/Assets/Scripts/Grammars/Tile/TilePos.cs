namespace Grammars.Tile {
    public class TilePos {
        public int x { get; private set; }
        public int y { get; private set; }
        private int rotation;
        public int Rotation {
            get { return rotation; }
            set { rotation = (value % 4 + 4) % 4; }
        }
        internal TilePos(int x, int y, int rotation=0) {
            this.x = x;
            this.y = y;
            Rotation = rotation;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(this, obj)) {
                return true;
            }
            TilePos instance = obj as TilePos;
            if (instance == null) {
                return false;
            }
            return (x == instance.x && y == instance.y);
        }

        public override int GetHashCode() {
            return x.GetHashCode() ^ y.GetHashCode();
        }
    }
}
