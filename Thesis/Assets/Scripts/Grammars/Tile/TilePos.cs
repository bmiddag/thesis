namespace Grammars.Tiles {
    public class TilePos {
        public int x { get; private set; }
        public int y { get; private set; }
        private int rotation;
        private bool rotImportant;
        public int Rotation {
            get { return rotation; }
            set { rotation = (value % 4 + 4) % 4; }
        }
        public bool RotImportant {
            get { return rotImportant; }
            set { rotImportant = value; }
        }
        public TilePos(int x, int y, int rotation=0, bool rotImportant=false) {
            this.x = x;
            this.y = y;
            this.rotImportant = rotImportant;
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
            if (rotImportant != instance.rotImportant) return false;
            if (rotImportant) {
                return (x == instance.x && y == instance.y && rotation == instance.Rotation);
            } else {
                return (x == instance.x && y == instance.y);
            }
        }

        public override int GetHashCode() {
            unchecked {
                int hash = 17;
                hash = hash * 31 + x.GetHashCode();
                hash = hash * 31 + y.GetHashCode();
                if (rotImportant) hash = hash * 31 + rotation.GetHashCode();
                return hash;
            }
        }
    }
}
