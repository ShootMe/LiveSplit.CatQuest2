namespace LiveSplit.CatQuest2 {
    public class Chest {
        public string Guid;
        public bool Collected;

        public Chest Clone() {
            return new Chest() { Collected = Collected, Guid = Guid };
        }
        public override string ToString() {
            return $"(Guid={Guid})(Collected={Collected})";
        }
    }
}