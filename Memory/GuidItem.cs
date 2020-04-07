namespace LiveSplit.CatQuest2 {
    public class GuidItem {
        public string Guid;

        public GuidItem Clone() {
            return new GuidItem() { Guid = Guid };
        }
        public override string ToString() {
            return $"(Guid={Guid})";
        }
    }
}