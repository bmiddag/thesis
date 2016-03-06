using System;

namespace Util {
    public class Pair {
        public int x { get; private set; }
        public int y { get; private set; }
        internal Pair(int x, int y) {
            this.x = x;
            this.y = y;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(this, obj)) {
                return true;
            }
            Pair instance = obj as Pair;
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
