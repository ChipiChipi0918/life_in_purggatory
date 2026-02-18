using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ArgumentEvidenceButton : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    private EvidenceManager.Evidence data;

    private bool isHovered = false;
    private bool isSelected = false;

    private Color defaultColor;

    [Header("UI")]
    public TextMeshProUGUI argumentEvidenceName;
    public Image argumentEvidenceImg;

    private RectTransform rect;
    private Image bg;

    private const float SHOW_X = -42f;
    private const float HIDE_X = 188.8423f;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        bg = GetComponent<Image>();
        defaultColor = bg.color;
    }

    public void Init(EvidenceManager.Evidence evidenceData)
    {
        data = evidenceData;
        argumentEvidenceName.text = data.evidenceName;
        argumentEvidenceImg.sprite = data.evidenceImage;
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
        if (isSelected)
        {
            Deselect();
        }
        else
        {
            isSelected = true;
            bg.color = Color.red;

            SoundManager.instance.UiSelect();
            ArgumentManager.instance.SetSelectedEvidence(argumentEvidenceName.text);
        }
        ArgumentManager.instance.ArgumentSelectEvidence(this);
    }

    public void Deselect()
    {
        isSelected = false;
        bg.color = defaultColor;
        ArgumentManager.instance.SetSelectedEvidence("");

        if (!isHovered)
            Hide();
    }
}
