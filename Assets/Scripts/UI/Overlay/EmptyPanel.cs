namespace Game.UI
{
    /// <summary>
    /// Placeholder panel for the empty Map tab (doc: "leave Map empty, author fills it later").
    /// Satisfies the <see cref="MenuPanel"/> contract so the tab bar can include Map today.
    /// </summary>
    public class EmptyPanel : MenuPanel
    {
        [UnityEngine.SerializeField] private string tabLabel = "Map";
        public override string TabLabel => tabLabel;
    }
}
