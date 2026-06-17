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
        characterImgUi.sprite = characterImg;
        characterNameUi.text = characterName;
        characterExplanationUi.text = characterExplanation;
    }
}