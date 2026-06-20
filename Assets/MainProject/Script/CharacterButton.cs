using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterButton : MonoBehaviour
{
    [Header("Character Data")]
    [SerializeField] private Sprite characterImg;
    [SerializeField] private string characterName;
    [SerializeField, TextArea] private string characterExplanation;

    [Header("UI")]
    [SerializeField] private Image characterImgUi;
    [SerializeField] private TextMeshProUGUI characterNameUi;
    [SerializeField] private TextMeshProUGUI characterExplanationUi;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(SetCharacterInfo);
    }

    private void SetCharacterInfo()
    {

        SoundManager.instance.UiSelect();

        characterImgUi.sprite = characterImg;
        characterExplanationUi.text = characterExplanation;

        string colorCode = "#D9D9D9";

        if (DialogueDirector.instance != null)
        {
            if (DialogueDirector.instance.characterConfig.TryGetValue(characterName, out var data))
                colorCode = data.colorCode;
            else
                colorCode = DialogueDirector.instance.characterConfig["Default"].colorCode;
        }

        string firstChar = characterName.Substring(0, 1);
        string restName = characterName.Length > 1 ? characterName.Substring(1) : "";

        characterNameUi.text =
            $"<size=180%><color={colorCode}>{firstChar}</color></size>{restName}";
    }
}