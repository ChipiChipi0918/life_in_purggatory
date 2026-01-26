using DG.Tweening;
using KoreanTyper;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Unity.Collections.AllocatorManager;

using System.Text.RegularExpressions;

public static class ArgumentTextFormatter
{
    // 정답 키워드: |*내용*|
    private static readonly Regex CorrectKeywordRegex = new Regex(@"\|\*(.*?)\*\|");
    // 일반 키워드: |내용|
    private static readonly Regex NormalKeywordRegex = new Regex(@"\|(.*?)\|");

    public static string Format(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // 1. 정답 키워드 먼저 처리
        string formatted = CorrectKeywordRegex.Replace(text, match =>
        {
            string keyword = match.Groups[1].Value;
            // 클릭 가능한 타겟임을 알리기 위해 cmd: 접두사 유지
            return $"<link=\"cmd:{keyword}\"><b><color=#FF4444>{keyword}</color></b></link>";
        });

        // 2. 남은 일반 키워드 처리
        formatted = NormalKeywordRegex.Replace(formatted, match =>
        {
            string keyword = match.Groups[1].Value;
            return $"<link=\"cmd2:{keyword}\"><b><color=#FF4444>{keyword}</color></b></link>";
        });

        return formatted;
    }
}


public class ArgumentManager : MonoBehaviour, IPointerClickHandler
{
    public static ArgumentManager instance;

    #region Enums & Config
    // 🔥 상태 관리: 현재 게임이 어떤 상태인지 명확히 정의
    public enum FlowState
    {
        Idle,               // 대기
        Dialogue_Typing,    // 일반 대화 타이핑 중
        Dialogue_Wait,      // 일반 대화 출력 완료 (클릭 대기)
        Argument_Loop,      // 논의 진행 중 (자동 재생)
        Argument_EndWait,   // 논의 한 사이클 종료 후 대기 (재시작 또는 진행)
        Choice,           // 선택지 화면
        PlaceSelection // 🔥 [추가] 장소 지적 상태
    }

    // 캐릭터 설정 데이터 (하드코딩 제거용)
    private readonly Dictionary<string, (float camPos, string colorCode)> characterConfig = new Dictionary<string, (float, string)>()
    {
        { "유은하", (0f, "#FFE2A0") },
        { "백현",   (-80f, "#0E432D") },
        { "유설희", (-60f, "#8F8F8F") },
        { "다니엘", (-40f, "#E7A300") },
        { "정희영", (-20f, "#9B2BFF") },
        { "장현우", (20f, "#CB1B00") },
        { "천주연", (40f, "#FFE945") },
        { "정태준", (60f, "#1572FF") },
        { "서진랑", (80f, "#5E3200") },
        { "Default", (0f, "#D9D9D9") } // 기본값
    };
    #endregion

    #region Variables
    [Header("State Info")]
    [SerializeField] private FlowState currentState = FlowState.Idle;

    public FlowState CurrentState => currentState;
    public bool IsArgumentMode => currentState == FlowState.Argument_Loop || currentState == FlowState.Argument_EndWait;

    [Header("Events")]
    public System.Action OnAllDialogueFinished;

    [Header("Data")]
    public static List<ArgumentBlock> argumentBlocks;
    private List<DialogueLine> currentLines;
    private int lineIndex = 0;          // 전체 대화 인덱스
    private int currentBlockIndex = 0;  // 논의 블록 인덱스
    private int argumentLineIndex = 0;  // 논의 내부 라인 인덱스

    [Header("Choice UI")]
    public GameObject choicePanel;
    public GameObject choiceButtonPrefab;
    public Transform choiceButtonParent;

    [Header("Argument UI")]
    public TMP_Text argumentTextLeft;
    public TMP_Text argumentTextRight;
    private TMP_Text activeArgumentText;
    public float argumentDuration = 0.5f;
    public Transform argumentCamTransform;
    public Transform argumentEvidenceButtonParent;
    public GameObject argumentEvidenceButtonPrefab;

    [Header("Place Selection Logic")]
    public string currentSelectedPlaceName; // 🔥 [추가] 플레이어가 클릭한 장소 이름
    private string currentPlaceAnswer;      // 🔥 [추가] 현재 문제의 정답 장소 이름

    [Header("Evidence Logic")]
    public string selectedEvidenceName; // 플레이어가 선택한 증거
    public string correctEvidenceName;  // 현재 정답 증거

    [Header("Dialogue UI")]
    public GameObject dialoguePanel;
    public GameObject nameTagObj;
    public TMP_Text nameText;
    public TMP_Text dialogueText;
    public Slider textSlider;

    [Header("Typing Settings")]
    private bool isSkipTyping;
    private Coroutine typingRoutine;
    private Coroutine argumentRoutine;

    // 내부 변수
    private string currentRawText = "";
    private int currentLinkHoverIndex = -1;
    private bool lastUiAnimState = false;
    private bool isChoiceShowingWrongFeedback = false; // 오답 피드백 대사 중인지 체크
    private bool isMapPointOutShowingWrongFeedback = false; // 장소 지적 대사 중인지 체크
    private bool waitingExitDialogue = false;
    private ArgumentEvidenceButton currentSelected;
    #endregion

    private void Awake()
    {
        if (instance == null) instance = this;

        activeArgumentText = argumentTextLeft;
        argumentTextLeft.gameObject.SetActive(false);
        argumentTextRight.gameObject.SetActive(false);
        choicePanel.SetActive(false);
    }

    private void Update()
    {
        // UI 애니메이션 체크 후 다이얼로그 복구
        HandleUiAnimationState();

        // 마우스 호버 체크 (논의 중일 때만)
        if (IsArgumentMode) CheckHover();

        // 클릭 입력 처리
        if (Input.GetMouseButtonDown(0))
        {
            HandleInput();
        }
    }

    private void LateUpdate()
    {
        // 타이핑 스킵 처리
        if (currentState == FlowState.Dialogue_Typing && Input.GetMouseButtonDown(0))
        {
            isSkipTyping = true;
        }
    }

    #region Core Flow (State Machine)

    public void PlayLines(List<DialogueLine> dialogueLines)
    {
        if (dialogueLines == null || dialogueLines.Count == 0) return;

        currentLines = dialogueLines;
        lineIndex = 0;

        StopAllCoroutines();
        ResetUI();

        PlayNext();
    }

    public void PlayNext()
    {
        // UI 애니메이션 중이거나 호텔 정보 창이 켜져있으면 대기
        if (UiManager.instance.isUiAnim || UiManager.instance.isHotelInformation) return;

        // 1. 전체 대화 종료 체크
        if (lineIndex >= currentLines.Count)
        {
            EndAll();
            return;
        }

        DialogueLine line = currentLines[lineIndex];

        // 2. 대화 타입에 따른 분기
        if (line.type == DialogueType.Argument)
        {
            if (!IsArgumentMode) StartArgumentMode();
            else NextArgumentLine();
        }
        else if (line.type == DialogueType.PlaceSelection) // 🔥 [추가] 장소 지적 처리
        {
            lineIndex++; // 인덱스 증가
            StartPlaceSelection(line);
        }
        else if (line.isChoice)
        {
            lineIndex++;
            ShowChoice(line);
        }
        else // 일반 Dialogue
        {
            lineIndex++;
            ShowDialogue(line);
        }
    }

    private void HandleInput()
    {
        switch (currentState)
        {
            case FlowState.Dialogue_Wait:
                // 상황 A: 선택지 오답 피드백 중이었다면 선택지로 복귀
                if (isChoiceShowingWrongFeedback)
                {
                    ReturnToChoice();
                }
                // 상황 B: 장소 지적 피드백 중이었다면 선택지로 복귀
                if (isMapPointOutShowingWrongFeedback)
                {
                    ReturnToMapPointOut();
                }
                // 상황 C: "다시 들어보자" 대사가 끝난 후 클릭했다면 루프 재시작 🔥 (추가됨)
                else if (waitingExitDialogue)
                {
                    waitingExitDialogue = false;
                    RestartArgumentLoop();
                }
                // 상황 D: 일반적인 다음 대사 진행
                else
                {
                    PlayNext();
                }
                break;

            case FlowState.Argument_EndWait:
                // 상황 D: 논의 한 사이클 종료(ExitLine 출력 완료) 후 클릭 시 🔥 (수정됨)
                if (argumentEvidenceButtonParent == null) return;

                if (waitingArgumentEndText)
                {
                    waitingArgumentEndText = false;

                    // 재시작 전 안내 대사 출력
                    ShowDialogue(new DialogueLine
                    {
                        speaker = "유은하",
                        text = "(다시 한번 모두의 의견을 들어보자.)",
                        type = DialogueType.Dialogue
                    });

                    // 이 대사가 끝나면 다시 클릭했을 때 루프가 돌도록 플래그 설정
                    waitingExitDialogue = true;
                }
                break;
        }
    }

    // 선택지로 다시 되돌리는 메서드
    private void ReturnToChoice()
    {
        isChoiceShowingWrongFeedback = false;
        dialoguePanel.SetActive(false); // 오답 메시지 창 닫기
        choicePanel.SetActive(true);    // 다시 선택지 버튼들 보여주기
        currentState = FlowState.Choice; // 상태를 다시 선택지로 변경
    }

    // 장소 지적으로 다시 되돌리는 메서드
    private void ReturnToMapPointOut()
    {
        isMapPointOutShowingWrongFeedback = false;
        currentState = FlowState.PlaceSelection; // 상태를 다시 장소 지적으로 변경
    }
    #endregion

    #region Argument Logic (Non-stop Debate)

    private void StartArgumentMode()
    {
        // 1. 해당 논의 블록의 기초 데이터 세팅
        ArgumentBlock block = argumentBlocks[currentBlockIndex];
        correctEvidenceName = block.correctEvidence;
        selectedEvidenceName = null;

        Debug.Log($"논의 데이터 로드 완료! 정답: {correctEvidenceName}");

        // 🔥 [추가/수정] 첫 번째 대사의 방향에 따라 초기 카메라 회전 결정
        float initialRotation = 2.5f; // 기본값 (left)
        if (block.lines.Count > 0)
        {
            // 첫 번째 라인이 "right"라면 -2.5도로 시작
            if (block.lines[0].camFormat == "right")
            {
                initialRotation = -2.5f;
            }
        }

        // UiManager 호출 시 계산된 회전값 전달
        UiManager.instance.ArgumentUiOn(true, initialRotation);

        // 🔥 [추가] 증거품 버튼 생성 호출
        CreateArgumentEvidenceButtons(block);

        // 2. 루프 시작
        RestartArgumentLoop();
    }
    private void RestartArgumentLoop()
    {
        // 🔥 [추가] 논의 시작 UI 연출 실행 (Banner 애니메이션)
        UiManager.instance.ArgumentUiOn(true);

        // 대화창은 연출 중에 가려지도록 설정
        dialoguePanel.SetActive(false);

        // 상태 및 인덱스 초기화
        currentState = FlowState.Argument_Loop;
        argumentLineIndex = 0;
        waitingArgumentEndText = false;

        // 첫 번째 논의 대사 재생
        NextArgumentLine();
    }

    // 논의가 끝난 후 Hello World(재시작 대기) 상태인지 체크하는 플래그
    private bool waitingArgumentEndText = false;

    private void NextArgumentLine()
    {
        ArgumentBlock block = argumentBlocks[currentBlockIndex];

        // 🔥 논의 루프가 끝났을 때
        if (argumentLineIndex >= block.lines.Count)
        {
            foreach (Transform child in argumentEvidenceButtonParent)
            {
                child.GetComponent<ArgumentEvidenceButton>().Deselect(); //논의 증거품 Ui 자식(버튼)들 전부 선택 취소
            }

            UiManager.instance.ArgumentUiOn(false);

            argumentTextLeft.gameObject.SetActive(false);
            argumentTextRight.gameObject.SetActive(false);


            StartCoroutine(ArgumentEndUIEndShowDialogue(block));
            return;
        }

        // 🔥 논의 진행 중
        currentState = FlowState.Argument_Loop;
        DialogueLine line = block.lines[argumentLineIndex];
        argumentLineIndex++;

        // 카메라 및 텍스트 위치 설정
        SetupArgumentTextUI(line);
        

        // 연출 코루틴 시작
        if (argumentRoutine != null) StopCoroutine(argumentRoutine);
        argumentRoutine = StartCoroutine(PlayArgumentRoutine(line));
    }

    IEnumerator ArgumentEndUIEndShowDialogue(ArgumentBlock b)
    {
        yield return new WaitForSecondsRealtime(4.35f);

        // 한 사이클 종료 -> Exit Line 출력 후 대기 상태로 전환
        currentState = FlowState.Argument_EndWait;

        
        // Exit Line 출력
        waitingArgumentEndText = true;
        ShowDialogue(b.exitLine);
    }

    private void SetupArgumentTextUI(DialogueLine line)
    {
        argumentTextLeft.gameObject.SetActive(false);
        argumentTextRight.gameObject.SetActive(false);

        float xOffset = 0;
        float camZ = -10f; // 기본 Z값

        if (line.camFormat == "left")
        {
            activeArgumentText = argumentTextLeft;
            xOffset = 2.2f;
            // 2번째 라인부터는 약간 회전 연출 (원래 코드 유지)
            if (argumentLineIndex > 1) UiManager.instance.camRotate(new Vector3(0, 0, 2.5f), 0.5f);
        }
        else // right
        {
            activeArgumentText = argumentTextRight;
            xOffset = -2.2f;
            if (argumentLineIndex > 1) UiManager.instance.camRotate(new Vector3(0, 0, -2.5f), 0.5f);
        }

        MoveCam(line.speaker, xOffset);
        activeArgumentText.gameObject.SetActive(true);
    }

    IEnumerator PlayArgumentRoutine(DialogueLine line)
    {
        currentRawText = line.text;
        activeArgumentText.text = ArgumentTextFormatter.Format(currentRawText);

        CanvasGroup cg = activeArgumentText.GetComponent<CanvasGroup>();
        if (cg == null) cg = activeArgumentText.gameObject.AddComponent<CanvasGroup>();

        // 초기화
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        // 카메라 이동 대기 (이전 화자와 다르면 좀 더 기다림)
        yield return new WaitForSeconds(0.2f); // 단순화

        // 페이드 인
        float elapsed = 0f;
        while (elapsed < argumentDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0f, 1f, elapsed / argumentDuration);
            yield return null;
        }
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;

        // 텍스트 유지 시간 대기
        yield return new WaitForSeconds(line.textTime);

        // 다음 라인으로 (자동 진행)
        if (currentState == FlowState.Argument_Loop)
        {
            NextArgumentLine();
        }
    }

    public void ArgumentCorrect()
    {
        waitingArgumentEndText = false;
        waitingExitDialogue = false;
        isChoiceShowingWrongFeedback = false;

        currentState = FlowState.Idle;

        // 🔥 증거 버튼 전부 제거
        ClearAllEvidenceButtons();

        // UI 정리
        UiManager.instance.camRotate(Vector3.zero, 1);
        UiManager.instance.OnArgumentEvidence(false);

        argumentTextLeft.gameObject.SetActive(false);
        argumentTextRight.gameObject.SetActive(false);

        SkipToAfterExitLine();

        if (currentBlockIndex < argumentBlocks.Count - 1)
        {
            currentBlockIndex++;
        }

        PlayNext();
    }


    public void ArgumentSelectEvidence(ArgumentEvidenceButton button)
    {
        // 이미 다른 게 선택되어 있으면 해제
        if (currentSelected != null && currentSelected != button)
        {
            currentSelected.Deselect();
        }

        currentSelected = button;
    }

    private void ClearAllEvidenceButtons()
    {
        if (argumentEvidenceButtonParent == null) return;

        foreach (Transform child in argumentEvidenceButtonParent)
        {
            Destroy(child.gameObject);
        }

        currentSelected = null;
        selectedEvidenceName = null;
    }


    private void SkipToAfterExitLine()
    {
        if (currentLines == null) return;

        DialogueLine exitLine = argumentBlocks[currentBlockIndex].exitLine;

        int exitIndex = currentLines.FindIndex(lineIndex, l =>
            l.speaker == exitLine.speaker && l.text == exitLine.text);

        if (exitIndex != -1)
        {
            lineIndex = exitIndex + 1;
            Debug.Log($"점프 성공! 새로운 인덱스: {lineIndex}");
        }
        else
        {
            Debug.LogWarning("ExitLine을 찾지 못해 강제 탐색을 시도합니다.");
            while (lineIndex < currentLines.Count && currentLines[lineIndex].type == DialogueType.Argument)
            {
                lineIndex++;
            }
        }
    }

    private void CreateArgumentEvidenceButtons(ArgumentBlock block)
    {
        // 기존에 생성된 버튼 제거
        ClearAllEvidenceButtons();

        if (block.evidenceCandidates == null || block.evidenceCandidates.Count == 0) return;

        foreach (string evName in block.evidenceCandidates)
        {
            // 1. EvidenceManager에서 이름에 맞는 데이터 찾기
            EvidenceManager.Evidence data = EvidenceManager.Instance.evidence.Find(e => e.evidenceName == evName);

            // 만약 리스트에 없다면(획득하지 않은 증거라면) 새로 데이터를 생성하거나 스킵
            if (data == null)
            {
                Debug.LogWarning($"{evName} 데이터를 EvidenceManager에서 찾을 수 없습니다. 기본 이미지로 생성합니다.");
                // 선택 사항: 임시 데이터 생성
                Sprite img = EvidenceManager.Instance.GetEvidenceImage(evName);
                string desc = EvidenceManager.Instance.GetEvidenceExplanation(evName);
                data = new EvidenceManager.Evidence(evName, desc, img);
            }

            // 2. 프리팹 생성
            GameObject btnObj = Instantiate(argumentEvidenceButtonPrefab, argumentEvidenceButtonParent);

            // 3. ArgumentEvidenceButton 컴포넌트 가져와서 초기화 (중요: 여기서 이미지와 이름이 설정됨)
            ArgumentEvidenceButton evidenceBtn = btnObj.GetComponent<ArgumentEvidenceButton>();
            if (evidenceBtn != null)
            {
                evidenceBtn.Init(data);
            }

            // 4. 클릭 이벤트 연결
            Button btn = btnObj.GetComponent<Button>();
            string s = evName;
            btn.onClick.AddListener(() => {
                OnArgumentEvidenceButtonClicked(s, btnObj);
                // ArgumentEvidenceButton 내부의 Select를 호출하기 위해
                evidenceBtn.OnPointerClick(null);
            });
        }
    }
    // 🔥 [신규 함수] 증거 버튼 클릭 시 실행될 로직
    private void OnArgumentEvidenceButtonClicked(string evidenceName, GameObject buttonObj)
    {
        // 현재 선택된 증거 이름 저장
        selectedEvidenceName = evidenceName;
        Debug.Log($"증거 선택됨: {selectedEvidenceName}");

        // 시각적 피드백: 모든 버튼의 색상을 원래대로 돌리고 클릭한 것만 강조
        foreach (Transform child in argumentEvidenceButtonParent)
        {
            Image img = child.GetComponent<Image>();
            if (img != null) img.color = Color.white;
        }

        Image selectedImg = buttonObj.GetComponent<Image>();
        if (selectedImg != null) selectedImg.color = Color.yellow; // 선택된 버튼 강조

        // (선택 사항) 사운드 효과
        // SoundManager.instance.PlaySE("Click");
    }
    #endregion

    #region Dialogue System (General)

    private void ShowDialogue(DialogueLine line)
    {
        currentState = FlowState.Dialogue_Typing;
        dialoguePanel.SetActive(true);

        // 1. 카메라 & 증거품 처리
        TpCam(line.speaker);
        ProcessEvidenceEffects(line);
        ProcessScreenEffects(line);

        // 2. UI 텍스트 설정
        UpdateNameTag(line.speaker);

        // 3. 타이핑 시작
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = StartCoroutine(TypeRoutine(line.speaker, line.text));
    }

    IEnumerator TypeRoutine(string speaker, string text)
    {
        yield return new WaitForSecondsRealtime(0.02f);

        dialogueText.text = "";
        isSkipTyping = false;
        textSlider.maxValue = 1f;
        textSlider.value = 0f;

        // 타이핑 속도 계산
        float timePerChar = Mathf.Clamp(1.0f / text.Length, 0.015f, 0.05f);

        for (int i = 0; i < text.Length; i++)
        {
            dialogueText.text = text.Substring(0, i + 1);
            if (i % 2 == 1) SoundManager.instance.EunhaVoice(); // 보이스 재생 (간소화)

            yield return new WaitForSeconds(timePerChar);

            if (isSkipTyping)
            {
                dialogueText.text = text;
                break;
            }
        }

        // 타이핑 완료
        textSlider.value = 1f;

        // 만약 논의 종료 대기 상태였다면 상태 변경하지 않음 (Argument_EndWait 유지)
        if (waitingArgumentEndText)
        {
            currentState = FlowState.Argument_EndWait;
        }
        else
        {
            currentState = FlowState.Dialogue_Wait;
        }
    }
    public void ForceSkip()
    {
        // 타이핑 중이면 타이핑 즉시 완료
        if (currentState == FlowState.Dialogue_Typing)
        {
            isSkipTyping = true;
        }
        // 대기 중이면 다음 대사로 넘김
        else if (currentState == FlowState.Dialogue_Wait)
        {
            PlayNext();
        }
    }
    #endregion

    #region Interaction (Hover & Click)

    private void CheckHover()
    {
        if (activeArgumentText == null || activeArgumentText.text == "") return;

        Camera cam = (activeArgumentText.canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            ? null : activeArgumentText.canvas.worldCamera;

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(activeArgumentText, Input.mousePosition, cam);

        if (linkIndex != currentLinkHoverIndex)
        {
            currentLinkHoverIndex = linkIndex;
            UpdateHoverTextEffect();
        }
    }

    private void UpdateHoverTextEffect()
    {
        string formattedText = ArgumentTextFormatter.Format(currentRawText);

        if (currentLinkHoverIndex != -1)
        {
            // textInfo에서 현재 호버된 링크 정보를 가져옴
            TMP_LinkInfo info = activeArgumentText.textInfo.linkInfo[currentLinkHoverIndex];
            string id = info.GetLinkID();

            if (id.StartsWith("cmd:"))
            {
                string keyword = info.GetLinkText(); // 링크에 걸린 텍스트 직접 추출

                // 호버 시 시각적 변화 (더 진한 빨간색으로 변경)
                string normalTag = $"<link=\"cmd:{keyword}\"><b><color=#FF4444>{keyword}</color></b></link>";
                string hoverTag = $"<link=\"cmd:{keyword}\"><b><color=#CC0000>{keyword}</color></b></link>";

                formattedText = formattedText.Replace(normalTag, hoverTag);
            }
            else if (id.StartsWith("cmd2:"))
            {
                string keyword = info.GetLinkText(); // 링크에 걸린 텍스트 직접 추출

                // 호버 시 시각적 변화 (더 진한 빨간색으로 변경)
                string normalTag = $"<link=\"cmd2:{keyword}\"><b><color=#FF4444>{keyword}</color></b></link>";
                string hoverTag = $"<link=\"cmd2:{keyword}\"><b><color=#CC0000>{keyword}</color></b></link>";

                formattedText = formattedText.Replace(normalTag, hoverTag);
            }
        }
        activeArgumentText.text = formattedText;
    }

    // IPointerClickHandler 인터페이스 구현
    public void OnPointerClick(PointerEventData eventData)
    {
        // 논의 모드가 아니면 무시
        if (!IsArgumentMode) return;

        Camera cam = (activeArgumentText.canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            ? null : activeArgumentText.canvas.worldCamera;

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(activeArgumentText, eventData.position, cam);
        if (linkIndex == -1) return;

        string id = activeArgumentText.textInfo.linkInfo[linkIndex].GetLinkID();
        if (id.StartsWith("cmd:"))
        {
            // 키워드 클릭 로직
            CorrectProcessKeywordClick();
        }
        if (id.StartsWith("cmd2:"))
        {
            // 키워드 클릭 로직
            WorngProcessKeywordClick();
        }
    }

    private void CorrectProcessKeywordClick()
    {
        if (string.IsNullOrEmpty(selectedEvidenceName))
        {
            Debug.Log("증거품을 먼저 선택하세요.");
            UiManager.instance.Shaking(0.3f);
            return;
        }

        if (selectedEvidenceName == correctEvidenceName)
        {
            Debug.Log("이의 있음! (정답)");
            ArgumentCorrect();
        }
        else
        {
            Debug.Log("틀렸습니다.");
            UiManager.instance.Shaking(0.4f);
        }
    }

    private void WorngProcessKeywordClick()
    {
        Debug.Log("틀렸습니다.");
        UiManager.instance.Shaking(0.4f);
    }

    #endregion

    #region Helper Methods (Camera, Effect, Data)

    private void MoveCam(string name, float xOffset)
    {
        float baseX = GetCharacterPos(name);
        argumentCamTransform.DOMoveX(baseX + xOffset, 0.5f);
    }

    private void TpCam(string name)
    {
        float baseX = GetCharacterPos(name);
        argumentCamTransform.position = new Vector3(baseX, 0, -10);
    }

    private float GetCharacterPos(string name)
    {
        if (characterConfig.ContainsKey(name)) return characterConfig[name].camPos;
        return 0f;
    }

    private void UpdateNameTag(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            nameTagObj.SetActive(false);
            return;
        }

        nameTagObj.SetActive(true);
        string colorCode = characterConfig.ContainsKey(name) ? characterConfig[name].colorCode : characterConfig["Default"].colorCode;

        // 첫 글자 색상 처리
        string firstChar = name.Substring(0, 1);
        string restName = name.Substring(1);

        nameText.text = $"<size=180%><color={colorCode}>{firstChar}</color></size>{restName}";
    }

    private void ProcessEvidenceEffects(DialogueLine line)
    {
        if (EvidenceManager.Instance == null) return;

        if (!string.IsNullOrEmpty(line.addEvidence))
            EvidenceManager.Instance.AddEvidence(line.addEvidence);

        if (!string.IsNullOrEmpty(line.showEvidence))
            EvidenceManager.Instance.ShowEvidence(line.showEvidence);
    }

    private void ProcessScreenEffects(DialogueLine line)
    {
        // 이펙트 ID에 따른 처리 (Switch 문 추천)
        if (line.effect == 1) EffectManager.instance.CameraShake();
        else if (line.effect == 2) EffectManager.instance.Blood();
        else if (line.effect == 3) EffectManager.instance.ShakeAndBlood();
        else if (line.effect >= 10 && line.effect <= 20) EffectManager.instance.Objection(line.effect - 10);
        else if (line.effect == 100) EffectManager.instance.FadeIn();
        else if (line.effect == 101) EffectManager.instance.FadeOut();
    }

    private void HandleUiAnimationState()
    {
        // UI 애니메이션이 끝난 직후 다이얼로그가 꺼져있으면 켜주기
        if (lastUiAnimState && !UiManager.instance.isUiAnim)
        {
            if (currentState == FlowState.Dialogue_Typing || currentState == FlowState.Dialogue_Wait)
            {
                dialoguePanel.SetActive(true);
            }
        }
        lastUiAnimState = UiManager.instance.isUiAnim;
    }

    private void ResetUI()
    {
        activeArgumentText.text = "";
        nameText.text = "";
        dialogueText.text = "";
        textSlider.value = 0f;

        // 플래그 전면 초기화
        waitingArgumentEndText = false;
        waitingExitDialogue = false;
        isChoiceShowingWrongFeedback = false;
        selectedEvidenceName = null;
        currentSelected = null;
    }

    private void EndAll()
    {
        ResetUI();
        currentState = FlowState.Idle;
        OnAllDialogueFinished?.Invoke();
    }

    public void SetSelectedEvidence(string evidenceName)
    {
        selectedEvidenceName = evidenceName;
    }
    #endregion

    #region Choice System
    private void ShowChoice(DialogueLine line)
    {
        currentState = FlowState.Choice;
        choicePanel.SetActive(true);

        foreach (Transform child in choiceButtonParent) Destroy(child.gameObject);

        for (int i = 0; i < line.choices.Count; i++)
        {
            int index = i;
            GameObject btnObj = Instantiate(choiceButtonPrefab, choiceButtonParent);
            // 위치 조정 로직은 필요에 따라 Grid Layout Group 컴포넌트로 대체 권장

            TMP_Text btnText = btnObj.transform.GetChild(0).GetComponent<TMP_Text>();
            btnText.text = line.choices[i];

            btnObj.GetComponent<Button>().onClick.AddListener(() => OnChoiceSelected(line, index));
        }
    }

    private void OnChoiceSelected(DialogueLine line, int choiceIndex)
    {
        if (choiceIndex == line.correctIndex)
        {
            Debug.Log("정답!");
            isChoiceShowingWrongFeedback = false;
            choicePanel.SetActive(false);
            PlayNext();
        }
        else
        {
            Debug.Log("오답!");
            UiManager.instance.Shaking(0.5f);

            // 플래그 설정: 다음 클릭 시 PlayNext()가 아닌 선택지로 돌아가게 함
            isChoiceShowingWrongFeedback = true;

            // 대화창이 뜰 때 선택지 버튼이 가려지도록 잠시 비활성화 (선택 사항)
            choicePanel.SetActive(false);

            DialogueLine wrongLine = new DialogueLine
            {
                speaker = "유은하",
                text = "(이건 아닌 것 같아... 다시 한번 생각해보자.)",
                type = DialogueType.Dialogue
            };

            ShowDialogue(wrongLine);
        }
    }
    #endregion

    #region Place Selection Logic (장소 지적)

    private void StartPlaceSelection(DialogueLine line)
    {
        currentState = FlowState.PlaceSelection;

        // CSV의 두 번째 열(text)에 있는 값이 정답 장소 이름
        currentPlaceAnswer = line.text.Trim();
        currentSelectedPlaceName = "";

        Debug.Log($"[장소 지적 시작] 정답: {currentPlaceAnswer}");

        // 2. 🔥 [추가] UiManager를 통해 장소 지적 UI(지도 등)를 켬
        UiManager.instance.MapPointOutUiOn(true);
    }

    public void OnPlaceClicked(string placeName)
    {
        if (currentState != FlowState.PlaceSelection) return;

        // 현재 UI 애니메이션 중이면 클릭 방지
        if (UiManager.instance.isUiAnim) return;

        currentSelectedPlaceName = placeName;
        CheckPlaceAnswer();
    }

    private void CheckPlaceAnswer()
    {
        if (string.IsNullOrEmpty(currentSelectedPlaceName)) return;

        if (currentSelectedPlaceName.Trim() == currentPlaceAnswer)
        {
            Debug.Log("장소 지적 정답!");

            // 1. 🔥 [추가] 정답일 경우 UiManager를 통해 장소 지적 UI를 끔
            UiManager.instance.MapPointOutUiOn(false);

            isMapPointOutShowingWrongFeedback = false;

            // 2. 상태 초기화 및 다음 대사 진행
            currentState = FlowState.Idle;

            // UI가 들어가는 시간(약 1초)을 고려하여 약간의 딜레이 후 다음 대사 재생
            StartCoroutine(WaitAndPlayNext(1.1f));
        }
        else
        {
            Debug.Log("오답!");
            UiManager.instance.Shaking(0.5f);

            isMapPointOutShowingWrongFeedback = true;

            DialogueLine wrongLine = new DialogueLine
            {
                speaker = "유은하",
                text = "(여기는 아닌가봐... 다시 한번 생각해보자.)",
                type = DialogueType.Dialogue
            };

            ShowDialogue(wrongLine);
        }
    }

    // UI 연출이 끝난 뒤 대화를 이어가기 위한 헬퍼 코루틴
    private IEnumerator WaitAndPlayNext(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        PlayNext();
    }

    #endregion
}