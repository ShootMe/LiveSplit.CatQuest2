using System.ComponentModel;
namespace LiveSplit.CatQuest2 {
    public class SplitterSettings {
        public BindingList<Split> Autosplits = new BindingList<Split>();

        public SplitterSettings() {
            Autosplits.AllowNew = true;
            Autosplits.AllowRemove = true;
            Autosplits.AllowEdit = true;
        }
    }
}