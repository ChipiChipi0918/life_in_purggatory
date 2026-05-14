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

    private void Start()
    {
        rect = GetComponent<RectTransform>();
        bg = GetComponent<Image>();
        defaultColor = bg.color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        Show();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;

        // 선택된 상태가 아닐 때만 숨김
        if (!isSelected)
            Hide();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Select();
    }

    private void Show()
    {
        rect.DOKill(); // 이전 애니메이션 정지
        rect.DOAnchorPosX(SHOW_X, 0.25f).SetUpdate(true);
    }

    public void Hide()
    {
        rect.DOKill(); // 이전 애니메이션 정지
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
            Show(); // 선택 시 확실히 보여줌
        }

        ArgumentManager.instance.ArgumentSelectAct(this);
    }

    public void Deselect()
    {
        isSelected = false;
        bg.color = defaultColor;

        // 마우스가 버튼 위에 없다면 숨기고, 위에 있다면 Show 상태 유지
        if (!isHovered)
            Hide();
    }
}