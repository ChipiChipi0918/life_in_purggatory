using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArgumentEvidenceButton : MonoBehaviour
{
    private EvidenceManager.Evidence data;

    [Header("UI")]
    public TextMeshProUGUI argumentEvidenceName;
    public Image argumentEvidenceImg;

    public void Init(EvidenceManager.Evidence evidenceData)
    {
        data = evidenceData;
        argumentEvidenceName.text = data.evidenceName;
        argumentEvidenceImg.sprite = data.evidenceImage;
    }

    public void Click()
    {
        SoundManager.instance.UiSelect();
        ArgumentManager.instance.SetSelectedEvidence(argumentEvidenceName.text);
    }
}
