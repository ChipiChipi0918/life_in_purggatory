using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class EvidenceManager : MonoBehaviour
{
    public static EvidenceManager Instance;

    [System.Serializable]
    public class Evidence
    {
        public string evidenceName;
        public string evidenceExplanation;
        public Sprite evidenceImage;

        public Evidence(string name, string explanation, Sprite image)
        {
            evidenceName = name;
            evidenceExplanation = explanation;
            evidenceImage = image;
        }
    }

    [Header("Evidence Data")]
    public List<Evidence> evidence = new List<Evidence>();
    public List<Sprite> ch1_evidence = new List<Sprite>();

    [Header("Evidence UI")]
    public RectTransform scrollViewTransform;
    public Image evidenceImage;
    public TextMeshProUGUI evidenceNameText;
    public TextMeshProUGUI evidenceExplanationText;

    [Header("Add Evidence UI")]
    public GameObject addEvidence;
    public TextMeshProUGUI addEvidenceNameText;
    public Image addEvidenceImage;

    [Header("Show Evidence UI")]
    public GameObject showEvidence;
    public Image showEvidenceImage;

    [Header("Evidence Buttons")]
    public GameObject evidenceButtonGroup;
    public GameObject evidenceButton;

    private bool isUiAnim;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    // =============================
    // 증거 추가
    // =============================
    public void AddEvidence(string name)
    {
        if (evidence.Exists(e => e.evidenceName == name))
            return;

        Sprite img = GetEvidenceImage(name);
        string explanation = GetEvidenceExplanation(name);

        Evidence newEvidence = new Evidence(name, explanation, img);
        evidence.Add(newEvidence);

        AddEvidenceButton(newEvidence);
        ShowAddEvidenceUI(newEvidence);

        Debug.Log($"증거품 추가됨: {name}");
    }

    // =============================
    // 증거 UI 갱신
    // =============================
    public void EvidenceUpdate(Evidence data)
    {
        evidenceImage.sprite = data.evidenceImage;
        evidenceNameText.text = data.evidenceName;
        evidenceExplanationText.text = data.evidenceExplanation;

        evidenceExplanationText.ForceMeshUpdate();
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollViewTransform); //스크롤뷰 강제 재계산
    }

    // =============================
    // Add Evidence 연출
    // =============================
    private void ShowAddEvidenceUI(Evidence data)
    {
        addEvidenceNameText.text = data.evidenceName;
        addEvidenceImage.sprite = data.evidenceImage;
        StartCoroutine(AddEvidenceCoroutine());
    }

    private IEnumerator AddEvidenceCoroutine()
    {
        isUiAnim = true;
        addEvidence.transform.DOMoveY(30, 1f).SetUpdate(true);
        yield return new WaitForSecondsRealtime(2.3f);
        addEvidence.transform.DOMoveY(58, 1f).SetUpdate(true);
        isUiAnim = false;
    }

    // =============================
    // 버튼 생성
    // =============================
    private void AddEvidenceButton(Evidence data)
    {
        GameObject e = Instantiate(evidenceButton, evidenceButtonGroup.transform);
        e.GetComponent<EvidenceButton>().Init(data);
    }

    // =============================
    // 데이터 매핑
    // =============================
    private Sprite GetEvidenceImage(string name)
    {
        if (name == "Add Evidence") return ch1_evidence[0];
        if (name == "권총") return ch1_evidence[1];
        if (name == "탄흔") return ch1_evidence[2];

        return ch1_evidence[0];
    }

    private string GetEvidenceExplanation(string name)
    {
        if (name == "권총") return "사건 현장에서 발견된 권총\n\n피해자의 권총으로 기존에 2발 장전 되어 있었으나\n현장에서 발견 당시 탄환 2발이 전부 사라져있었다.";
        if (name == "탄흔") return "땅에 박혀있던 총알의 흔적\n\n백현의 말에 따르면 발사된지 얼마 되지 않듯 하다.";

        return "설명이 존재하지 않는 증거다.";
    }

    // =============================
    // 증거 보이기
    // =============================
    public void ShowEvidence(string name)
    {
        if (evidence.Exists(e => e.evidenceName == name))
            return;

        Sprite img = GetEvidenceImage(name);
        string explanation = GetEvidenceExplanation(name);

        Evidence newEvidence = new Evidence(name, explanation, img);
        evidence.Add(newEvidence);

        ShowShowEvidenceUI(newEvidence);

        Debug.Log($"증거품 추가됨: {name}");
    }

    private void ShowShowEvidenceUI(Evidence data)
    {
        showEvidenceImage.sprite = data.evidenceImage;
        StartCoroutine(ShowEvidenceCoroutine());
    }

    private IEnumerator ShowEvidenceCoroutine()
    {
        isUiAnim = true;
        showEvidence.transform.DOScaleY(1, 0.75f).SetUpdate(true);
        yield return new WaitForSecondsRealtime(5f);
        showEvidence.transform.DOScaleY(0, 0.75f).SetUpdate(true);
        isUiAnim = false;
    }
}
