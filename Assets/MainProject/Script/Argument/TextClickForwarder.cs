using UnityEngine;
using UnityEngine.EventSystems;

public class TextClickForwarder : MonoBehaviour, IPointerClickHandler
{
    public ArgumentManager target;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (target != null)
            target.OnPointerClick(eventData);
    }
}
