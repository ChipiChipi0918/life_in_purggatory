using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EvidenceButton : MonoBehaviour
{
    private EvidenceManager.Evidence data;

    [Header("UI")]
    public TextMeshProUGUI evidenceName;
    public Image evidenceImg;

    public void Init(EvidenceManager.Evidence evidenceData)
    {
        data = evidenceData;
        evidenceName.text = data.evidenceName;
        evidenceImg.sprite = data.evidenceImage;
    }

    public void Click()
    {
        EvidenceManager.Instance.EvidenceUpdate(data);
    }
}
