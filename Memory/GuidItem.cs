namespace LiveSplit.CatQuest2 {
    public class GuidItem {
        public string Guid;

        public override bool Equals(object obj) {
            return obj is GuidItem item && item.Guid == Guid;
        }
        public override int GetHashCode() {
            return Guid.GetHashCode();
        }
        public override string ToString() {
            return $"(Guid={Guid})";
        }
    }
}