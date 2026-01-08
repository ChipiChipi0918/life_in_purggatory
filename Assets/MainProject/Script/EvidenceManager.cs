using System.Collections.Generic;
using UnityEngine;

public class EvidenceManager : MonoBehaviour
{
    public static EvidenceManager Instance;

    public List<string> evidence = new List<string>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    public void AddEvidence(string name)
    {
        if (evidence.Contains(name)) return; // 중복 방지 (선택)

        evidence.Add(name);
        Debug.Log($"증거품 추가됨: {name}");
    }
}
