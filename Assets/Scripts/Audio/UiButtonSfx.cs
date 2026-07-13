using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Drop-in hover/click sounds for any UI Selectable. Add next to a Button to get
/// the standard menu feedback without wiring anything else in the inspector.
/// </summary>
public class UiButtonSfx : MonoBehaviour,
    IPointerEnterHandler, ISelectHandler, IPointerClickHandler, ISubmitHandler
{
    [SerializeField] private bool playHover = true;
    [SerializeField] private bool playClick = true;

    public void OnPointerEnter(PointerEventData eventData) => Hover();
    public void OnSelect(BaseEventData eventData) => Hover();
    public void OnPointerClick(PointerEventData eventData) => Click();
    public void OnSubmit(BaseEventData eventData) => Click();

    private void Hover()
    {
        if (playHover)
            Sfx.Play(SfxId.UiHover);
    }

    private void Click()
    {
        if (playClick)
            Sfx.Play(SfxId.UiConfirm);
    }
}
