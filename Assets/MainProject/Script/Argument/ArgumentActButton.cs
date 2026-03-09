using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ArgumentActButton : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    public ArgumentManager.ActState actState;

    private EvidenceManager.Evidence data;

    public bool isHovered = false;
    public bool isSelected = false;

    private Color defaultColor;

    private RectTransform rect;
    private Image bg;

    private const float SHOW_X = 80f;
    private const float HIDE_X = 188.8423f;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        bg = GetComponent<Image>();
        defaultColor = bg.color;
    }

    // ======================
    // Pointer Events
    // ======================

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        Show();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;

        if (!isSelected)
            Hide();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Select();
    }

    // ======================
    // State Control
    // ======================

    private void Show()
    {
        rect.DOAnchorPosX(SHOW_X, 0.25f).SetUpdate(true);
    }

    public void Hide()
    {
        rect.DOAnchorPosX(HIDE_X, 0.25f).SetUpdate(true);
    }

    public void Select()
    {
        SoundManager.instance.UiSelect();
        if (isSelected)
        {
            ArgumentManager.instance.currentAct = ArgumentManager.ActState.None;
            Deselect();
        }
        else
        {
            isSelected = true;
            bg.color = Color.red;

            ArgumentManager.instance.currentAct = actState;
        }

        ArgumentManager.instance.ArgumentSelectAct(this);
    }

    public void Deselect()
    {
        isSelected = false;
        bg.color = defaultColor;

        Hide();
    }
}
