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

    public List<Sprite> ex_evidence = new List<Sprite>();
    public List<Sprite> ch1_evidence = new List<Sprite>();

    [Header("Evidence UI")]
    public RectTransform scrollViewTransform;
    public Image evidenceImage;
    public Image evidenceImage2;
    public TextMeshProUGUI evidenceNameText;
    public TextMeshProUGUI evidenceNameText2;
    public TextMeshProUGUI evidenceExplanationText;
    public TextMeshProUGUI evidenceExplanationText2;

    [Header("Add Evidence UI")]
    public GameObject addEvidencePrefab; // RectTransform 대신 프리팹 GameObject로 변경
    public Transform uiCanvasParent;    // 프리팹이 생성될 부모 캔버스 또는 Panel

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

    public void OnHotelInform()
    {
        if (evidence == null || evidence.Count == 0)
        {
            Debug.LogWarning("증거 리스트가 비어있음");
            return;
        }

        EvidenceUpdate(evidence[0]);
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
    // 증거 전부 삭제
    // =============================
    public void ClearEvidence()
    {
        // 1. 리스트 데이터 삭제
        evidence.Clear();

        // 2. 생성된 UI 버튼들 삭제
        if (evidenceButtonGroup != null)
        {
            // 부모(ButtonGroup) 아래에 있는 모든 자식 요소를 순회하며 파괴
            foreach (Transform child in evidenceButtonGroup.transform)
            {
                Destroy(child.gameObject);
            }
        }

        Debug.Log("모든 증거 데이터와 UI 버튼이 삭제되었습니다.");
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

    public void ArgumentEvidenceUpdate(Evidence data)
    {
        evidenceImage2.sprite = data.evidenceImage;
        evidenceNameText2.text = data.evidenceName;
        evidenceExplanationText2.text = data.evidenceExplanation;

        evidenceExplanationText2.ForceMeshUpdate();
    }

    // =============================
    // Add Evidence 연출
    // =============================
    private void ShowAddEvidenceUI(Evidence data)
    {
        // 프리팹 생성
        GameObject go = Instantiate(addEvidencePrefab, uiCanvasParent);
        StartCoroutine(AddEvidenceCoroutine(go, data));
    }

    private IEnumerator AddEvidenceCoroutine(GameObject go, Evidence data)
    {
        isUiAnim = true;

        // 프리팹 내의 컴포넌트 찾아오기
        RectTransform rect = go.GetComponent<RectTransform>();
        // 만약 프리팹 전용 스크립트가 없다면 직접 UI 컴포넌트 참조 (자식 오브젝트 이름 기준)
        TextMeshProUGUI nameText = go.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
        Image evidenceImg = go.transform.Find("EvidenceImage").GetComponent<Image>();

        // 데이터 설정
        nameText.text = data.evidenceName;
        evidenceImg.sprite = data.evidenceImage;


        // 연출 시작
        rect.DOAnchorPosY(-162, 1).SetUpdate(true);

        yield return new WaitForSecondsRealtime(2.3f);

        // 원래 위치로 돌아가는 연출 (또는 사라지는 연출)
        yield return rect.DOAnchorPosY(232, 1).SetUpdate(true).WaitForCompletion();

        // 프리팹 삭제
        Destroy(go);

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
    public Sprite GetEvidenceImage(string name)
    {
        if (name == "Add Evidence") return ex_evidence[0];
        if (name == "권총") return ex_evidence[1];
        if (name == "현장 사진1") return ex_evidence[3];
        if (name == "현장 사진2") return ex_evidence[2];
        if (name == "검") return ex_evidence[4];
        if (name == "금고") return ex_evidence[5];
        if (name == "빨간 봉랍") return ex_evidence[6];
        if (name == "소형 카메라") return ex_evidence[7];
        if (name == "기현상 - 사후재판") return ex_evidence[8];
        if (name == "기현상 - 402호의 특성") return ex_evidence[9];
        if (name == "기현상 - 정전") return ex_evidence[10];
        if (name == "도둑맞은 문서") return ex_evidence[11];

        if (name == "Add Evidence") return ch1_evidence[0];
        if (name == "시몬이 남긴 쪽지") return ch1_evidence[1];
        if (name == "로넌의 시체 사진") return ch1_evidence[2];
        if (name == "로넌의 권총") return ch1_evidence[3];
        if (name == "시몬의 부검 기록") return ch1_evidence[0];
        if (name == "탄흔") return ch1_evidence[4];

        return ch1_evidence[0];
    }

    public string GetEvidenceExplanation(string name)
    {
        if (name == "권총")
            return "로넌에게서 빌린 권총.";
        if (name == "현장 사진1")
            return "402호에 베란다에서 찍은 시체 사진\n목이 잘린체 몸통만 남아있다.\n목에서 피가 흐르고있다.\n카를로스의 말에 따르면 죽은지 3분도 안된 모양.";
        if (name == "현장 사진2")
            return "테라스에서 찍은 시체 사진\n목이 잘린체 머리만 남아있다.\n떨어진 위치에는 피가 터진듯 퍼져있다 아무래도 이곳에서 큰 충격을 받은 듯 한 모양.";
        if (name == "도둑맞은 문서")
            return "엘리나에게 받았다\n테라스에서 피해자의 머리와 함께 발견했다고 한다.";
        if (name == "검") //<--흉기아님 피같은거 묻어있음
            return "402호에서 발견한 검\n매우 무거워서 한손으로는 휘두를 수 없다.\n장식용이지만 사람을 일격에 죽이는데에는 적합하다.\n피로 추정되는 액체는 말라서 굳어있다.";
        if (name == "금고")
            return "402호에서 발견한 피해자의 금고\n원래 안에 도둑맞은 문서가 들어있었다고 한다.";
        if (name == "빨간 봉랍")
            return "열려있던 금고 내부에서 발견한 빨간 봉랍\n뭐... 당연하겠지만 아무래도 편지를 봉인할때 썼겠지..";
        if (name == "소형 카메라")
            return "402호에서 발견\n겉보기엔 평범한 소형 카메라처럼 보이지만\n사실은 녹화부터 장거리 화면 송출, 적외선(IR), 열화상, UV 모드까지 탑제되어 있는 국가 시설에서만 사용 가능한 최신형 카메라다.\n이것이 왜 이곳에 설치 되어 있는지는 불명.";
        if (name == "기현상 - 402호의 특성")
            return "다른 방의 열쇠를 402호에 열쇠구멍에 넣을 경우\n해당 열쇠의 방과 402호의 방 내부가 서로 바뀐다\n이 때문에 호텔 측에서는 왠만하면 402호 키를 주지 않지만 지금처럼 사람이 가득 찰 경우 402호 키를 넘기기도 했다고 한다";
        if (name == "기현상 - 사후재판")
            return "호탤 내부에서 살인사건이 일어날 경우\n모든 출입구가 봉쇄된다.\n이 상황에서는 밖으로 나가는 것 조차가 불가능 하며\n범인을 찾아 처형하기 전까진 나갈 수 없다.\n이상하게도 호텔 측에선 그닥 큰 문제를 삼고있지 않는다.";
        if (name == "기현상 - 정전")
            return "규칙적으로 매달 30일 오후 10시에는 정전이 일어난다.\n이때 같은 경우 밖에서 들어오는 빛들도 전부 차단되기 때문에 아무것도 보이지 않는다.\n또한 정전 상황에서는 반경 7m외의 소리는 들을 수 없었다.\n하지만 정전의 지속 시간은 길어봤자 1~2분이기 때문에 호텔 측에선 그닥 큰 문제를 삼고있지 않는다.";


        if (name == "시몬이 남긴 쪽지")
            return "시몬이 엘리나에게 남긴 쪽지.\n직접 쓴 손글씨로 쓰여있다.\n\n언제쯤 볼지는 모르겠지만\n만약에 이 쪽지를 보고 있다는 거라면 테라스로 잘 와 주었겠구나.\n미안하지만 탐정으로써 처리해야할 일이 생겨서 말이야.\n직접 얼굴 보지 못해서 아쉽네.\n내가 나중에 따로 찾아갈 테니 여기 호텔도 좀 둘러보고.\n다른 사람들과 인사라도 나눠둬.\n-최고의 명탐정 로넌이-";
        if (name == "로넌의 시체 사진")
            return "사건 현장에서 찍은 로넌의 시체 사진\n피해자 왼쪽 머리에 총상이 있다.\n그 외의 자세한 특징은 없다.";
        if (name == "로넌의 권총")
            return "피해자의 시체 왼쪽에서 발견된 권총\n\n피해자의 권총으로 기존에 2발 장전 되어 있었으나\n현장에서 발견 당시 탄환 2발이 전부 사라져있었다.";
        if (name == "시몬의 부검 기록")
            return "피해자가 살해된 시각은 시체 발견 시각과 거의 동일하다.\n즉 시체 발견 직전 들린 총성에 의해 살해당한 모양.\n때문에 총상으로 인한 즉사로 추정.";
        if (name == "탄흔")
            return "땅에 박혀있던 총알의 흔적\n\n백현의 말에 따르면 발사된지 얼마 되지 않은 듯 하다.";

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

        Debug.Log($"증거품 보여짐: {name}");
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
