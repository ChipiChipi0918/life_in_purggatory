using TMPro;
using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(Button))]
public class SaveLoadButton : MonoBehaviour
{
    [SerializeField] private bool isSaveButton = true;
    [SerializeField] private int slotNumber = 0;

    [SerializeField] protected Sprite UseSlot;
    [SerializeField] protected Sprite NoneSlot;

    private Button button;
    private TMP_Text text;
    private Image image;
    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnButtonClick);

        image = GetComponent<Image>();
        text = GetComponentInChildren<TMP_Text>();

        RefreshText();
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(OnButtonClick);
    }

    private void OnButtonClick()
    {
        if (isSaveButton)
        {
            SaveManager.Instance.Save(slotNumber);
            RefreshText();   // ¿˙¿Â »ƒ ¡ÔΩ√ ∞ªΩ≈
        }
        else
        {
            SaveManager.Instance.Load(slotNumber);
        }
    }

    public void RefreshText()
    {
        if (text == null)
            return;

        SaveData data = SaveManager.Instance.GetSaveData(slotNumber);

        if (data == null)
        {
            text.text = "∫Û ΩΩ∑‘";

            if (image != null)
                image.sprite = NoneSlot;
        }
        else
        {
            text.text = $"¿˙¿Âµ \nLine {data.lineIndex}";

            if (image != null)
                image.sprite = UseSlot;
        }
    }
}