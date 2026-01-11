using System.Collections.Generic;
using UnityEngine;

public class EvidenceManager : MonoBehaviour
{
    public static EvidenceManager Instance;

    public List<string> evidence = new List<string>();

    public List<Sprite> ch1_evidence = new List<Sprite>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    public void AddEvidence(string name)
    {
        if (evidence.Contains(name)) return; // ¡ﬂ∫π πÊ¡ˆ (º±≈√)

        EvidenceImageUpdate(name);

        UiManager.instance.AddEvidence(name);
        evidence.Add(name);
        Debug.Log($"¡ı∞≈«∞ √ﬂ∞°µ : {name}");
    }

    private void EvidenceImageUpdate(string name)
    {

        Sprite nowImage = ch1_evidence[0];

        if (name == "Add Evidence") nowImage = ch1_evidence[0];
        else if (name == "±«√—") nowImage = ch1_evidence[1];
        else if (name == "≈∫»Á") nowImage = ch1_evidence[2];

        UiManager.instance.EvidenceImageUpdate(nowImage);
    }
}
