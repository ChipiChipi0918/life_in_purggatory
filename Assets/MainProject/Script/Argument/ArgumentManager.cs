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
    private static readonly Regex CorrectKeywordRegex = new Regex(@"\|\*(.*?)\*\|");
    private static readonly Regex NormalKeywordRegex = new Regex(@"\|(.*?)\|");

    // currentAct를 받아서 초기 색상을 결정합니다.
    public static string Format(string text, ArgumentManager.ActState act)
    {
        if (string.IsNullOrEmpty(text)) return text;
        string colorCode = GetColorByAct(act);

        // 정답 키워드: link가 color와 b를 감싸도록 수정
        string formatted = CorrectKeywordRegex.Replace(text, match => {
            string keyword = match.Groups[1].Value;
            return $"<link=\"cmd:{keyword}\"><color={colorCode}><b>{keyword}</b></color></link>";
        });

        // 일반 키워드
        formatted = NormalKeywordRegex.Replace(formatted, match => {
            string keyword = match.Groups[1].Value;
            return $"<link=\"cmd2:{keyword}\"><color={colorCode}><b>{keyword}</b></color></link>";
        });

        return formatted;
    }
    // 상태별 색상을 관리하는 헬퍼 함수
    public static string GetColorByAct(ArgumentManager.ActState act)
    {
        return act switch
        {
            ArgumentManager.ActState.None => "#575757",
            ArgumentManager.ActState.counterargument => "#E7D134",
            ArgumentManager.ActState.agreement => "#44FFE6",
            ArgumentManager.ActState.perjury => "#FF4444",
            _ => "#575757"
        };
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
        Argument_Confirm,   // 논의 확인 팝업 Ui 여부
        Argument_EndWait,   // 논의 한 사이클 종료 후 대기 (재시작 또는 진행)
        Choice,           // 선택지 화면
        PlaceSelection // 🔥 [추가] 장소 지적 상태
    }
    public enum ActState
    {
        None,
        counterargument,//반론
        agreement, //찬성
        perjury //위증
    }

    // 캐릭터 설정 데이터 (하드코딩 제거용)
    private readonly Dictionary<string, (float camPos, string colorCode)> characterConfig = new Dictionary<string, (float, string)>()
    {
        { "엘리나", (0f, "#FFE2A0") },
        { "시몬",   (-80f, "#0E432D") },
        { "실비아", (-60f, "#8F8F8F") },
        { "다니엘", (-40f, "#E7A300") },
        { "넬리", (-20f, "#9B2BFF") },
        { "에릭", (20f, "#CB1B00") },
        { "셀린", (40f, "#FFE945") },
        { "로넌", (60f, "#1572FF") },
        { "카를로스", (80f, "#5E3200") },
        { "리디아", (-80f, "#EAEAEA") },
        { "리네", (100f, "#EAEAEA") },
        { "총지배인", (100f, "#D9D9D9") },
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
    public Transform argumentActButtonParent;
    public GameObject argumentEvidenceButtonPrefab;
    public TMP_Text currentActText;

    [Header("Confirm UI")]
    private string pendingLinkID;

    [Header("Argument Progress UI")]
    public TMP_Text progressText; // "1 / 5" 형태로 표시할 텍스트 UI
    private int totalArgumentLines; // 현재 논의의 총 라인 수

    [Header("Place Selection Logic")]
    public string currentSelectedPlaceName; // 🔥 [추가] 플레이어가 클릭한 장소 이름
    private string currentPlaceAnswer;      // 🔥 [추가] 현재 문제의 정답 장소 이름

    [Header("Evidence Logic")]
    public string selectedEvidenceName; // 플레이어가 선택한 증거
    public string correctEvidenceName;  // 현재 정답 증거
    public ActState currentAct;

    [Header("Dialogue UI")]
    public GameObject dialoguePanel;
    public GameObject nameTagObj;
    public TMP_Text nameText;
    public TMP_Text dialogueText;
    public Slider textSlider;
    public GameObject NextImg;

    [Header("Logue")]
    public GameObject logueParent;
    public GameObject logueBox;

    [Header("Typing Settings")]
    private bool isSkipTyping;
    private Coroutine typingRoutine;
    private Coroutine argumentRoutine;


    // 내부 변수
    private Coroutine argumentCoroutine;
    private string currentRawText = "";
    private int currentLinkHoverIndex = -1;
    private bool lastUiAnimState = false;
    private bool isChoiceShowingWrongFeedback = false; // 오답 피드백 대사 중인지 체크
    private bool isMapPointOutShowingWrongFeedback = false; // 장소 지적 대사 중인지 체크
    private bool waitingExitDialogue = false;
    private bool isObjectionAnim = false;
    private ArgumentEvidenceButton currentEcidenceButtonSelected;
    private ArgumentActButton currentActButtonSelected;
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
        if (Input.GetMouseButtonDown(0)) HandleInput();
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
        // UI 애니메이션 중이거나 호텔 정보 창이 켜져있거나 Ui가 꺼져있으면 예외처리
        if (UiManager.instance.isUiAnim || UiManager.instance.isHotelInformation || UiManager.instance.isLogue || UiManager.instance.isUiOff || isObjectionAnim) return;

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
                // 순차적으로 우선순위를 따져서 하나만 실행합니다.
                if (isChoiceShowingWrongFeedback)
                {
                    ReturnToChoice();
                }
                else if (isMapPointOutShowingWrongFeedback)
                {
                    ReturnToMapPointOut();
                }
                else if (waitingExitDialogue)
                {
                    waitingExitDialogue = false;
                    RestartArgumentLoop();
                }
                else
                {
                    // 위의 특수 상황이 아닐 때만 다음 대사를 재생합니다.
                    PlayNext();
                }
                break;

            case FlowState.Argument_EndWait:
                if (argumentEvidenceButtonParent == null) return;

                if (waitingArgumentEndText)
                {
                    waitingArgumentEndText = false;
                    ShowDialogue(new DialogueLine
                    {
                        speaker = "엘리나",
                        text = "(다시 한번 모두의 의견을 들어보자.)",
                        type = DialogueType.Dialogue
                    });
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

        totalArgumentLines = block.lines.Count;
        UpdateProgressUI(0); // 초기화 (0 또는 1로 시작)

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
            foreach (Transform child in argumentActButtonParent)
            {
                Debug.Log("DW");
                child.GetComponent<ArgumentActButton>().Deselect(); //논의 행동 Ui 자식(버튼)들 전부 선택 취소
            }

            UiManager.instance.ArgumentUiOn(false);

            argumentTextLeft.gameObject.SetActive(false);
            argumentTextRight.gameObject.SetActive(false);


            StartCoroutine(ArgumentEndUIEndShowDialogue(block));
            return;
        }
        UpdateProgressUI(argumentLineIndex + 1);

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
    private void UpdateProgressUI(int currentStep)
    {
        if (progressText != null)
        {
            // 예: "1 / 8" 형식으로 출력
            progressText.text = $"{currentStep} / {totalArgumentLines}";
        }
    }

    IEnumerator ArgumentEndUIEndShowDialogue(ArgumentBlock b)
    {
        float waitTime = Mathf.Max(0f, 4.35f - argumentDuration);
        yield return new WaitForSecondsRealtime(waitTime);

        currentState = FlowState.Argument_EndWait;

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

        DialogueDirector.instance.MoveArgumentCam(line.speaker, xOffset);

        CanvasGroup cg = activeArgumentText.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
        Debug.Log($"Raycast:{cg.blocksRaycasts}, Interactable:{cg.interactable}, Alpha:{cg.alpha}");

        activeArgumentText.gameObject.SetActive(true);
    }

    IEnumerator PlayArgumentRoutine(DialogueLine line)
    {
        currentRawText = line.text;
        activeArgumentText.text = ArgumentTextFormatter.Format(currentRawText, currentAct);

        CanvasGroup cg = activeArgumentText.GetComponent<CanvasGroup>();
        if (cg == null) cg = activeArgumentText.gameObject.AddComponent<CanvasGroup>();

        // 1. 초기화 (알파 0, 클릭 방지)
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        // 🔥 [추가] TMP maxVisibleCharacters 초기화 (타이핑 준비)
        activeArgumentText.ForceMeshUpdate(); // 텍스트 메시 강제 업데이트
        int totalVisibleCharacters = activeArgumentText.textInfo.characterCount;
        activeArgumentText.maxVisibleCharacters = 0; // 처음에 글자 안 보임

        yield return new WaitForSeconds(0.45f); // 기존 대기 시간 유지


        // 2. 🔥 [통합] Fade In + Typing 동시 진행
        float elapsed = 0f;

        // 타이핑 속도 계산 (기본 다이얼로그와 동일)
        float timePerChar = Mathf.Clamp(1.0f / totalVisibleCharacters, 0.015f, 0.05f);
        float typingTimer = 0f;
        int currentTypeIndex = 0;

        // 페이드 인이 끝나거나 타이핑이 끝날 때까지 루프
        while (elapsed < argumentDuration || currentTypeIndex <= totalVisibleCharacters)
        {
            // A. 페이드 인 계산
            if (elapsed < argumentDuration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(0f, 1f, elapsed / argumentDuration);
            }
            else
            {
                cg.alpha = 1f; // 페이드 시간 지나면 완전히 보임
            }

            // B. 타이핑 계산
            if (currentTypeIndex <= totalVisibleCharacters)
            {
                typingTimer += Time.deltaTime;
                if (typingTimer >= timePerChar)
                {
                    typingTimer = 0f;
                    activeArgumentText.maxVisibleCharacters = currentTypeIndex;
                    currentTypeIndex++;

                    if (currentTypeIndex % 2 == 1 && line.speaker != "") SoundManager.instance.EllinaVoice();
                }
            }

            yield return null;
        }

        // 3. 완료 처리 (확정)
        cg.alpha = 1f;
        activeArgumentText.maxVisibleCharacters = totalVisibleCharacters; // 전부 보이게 확정

        // 대사가 다 쳐진 후에만 클릭 가능하도록 설정
        cg.interactable = true;
        cg.blocksRaycasts = true;


        // 4. [유지] 타이머 대기 로직 (상태 체크형)
        float textTimer = 0f;
        while (textTimer < line.textTime)
        {
            if (currentState == FlowState.Argument_Loop)
            {
                textTimer += Time.deltaTime;
            }
            yield return null;
        }

        // 5. 🔥 [추가] 대사 종료 시 Fade Out
        elapsed = 0f;
        while (elapsed < argumentDuration)
        {
            // 논의 루프 상태일 때만 페이드 아웃 진행
            if (currentState == FlowState.Argument_Loop)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, elapsed / argumentDuration);
            }
            yield return null;
        }
        cg.alpha = 0f; // 완전히 가림


        // 6. 다음 라인 진행
        if (currentState == FlowState.Argument_Loop)
        {
            NextArgumentLine();
        }
    }

    // 4. OnPointerClick 수정 (즉시 실행 대신 확인창 띄우기)
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsArgumentMode || currentAct == ActState.None || currentState == FlowState.Argument_Confirm) return;

        Camera cam = (activeArgumentText.canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            ? null : activeArgumentText.canvas.worldCamera;

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(activeArgumentText, eventData.position, cam);
        if (linkIndex == -1) return;

        // 클릭 정보 저장 및 상태 변경
        pendingLinkID = activeArgumentText.textInfo.linkInfo[linkIndex].GetLinkID();
        currentState = FlowState.Argument_Confirm;

        // UiManager를 통해 확인창 표시
        UiManager.instance.ShowArgumentConfirmUi(true);
    }

    //확인창 버튼 연결
    public void ConfirmPointOut()
    {
        UiManager.instance.ShowArgumentConfirmUi(false);

        if (pendingLinkID.StartsWith("cmd:"))
        {
            ArgumentBlock block = argumentBlocks[currentBlockIndex];

            // 🔥 행동과 증거가 모두 맞는지 검사
            bool isEvidenceCorrect = (selectedEvidenceName == correctEvidenceName);
            bool isActCorrect = (currentAct == block.actionType); // 상황에 맞게 비교

            if (isEvidenceCorrect && isActCorrect)
            {
                // 🎯 정답일 때만 코루틴을 멈추고 탈출합니다.
                if (argumentCoroutine != null)
                {
                    StopCoroutine(argumentCoroutine);
                    argumentCoroutine = null;
                }

                CorrectProcessKeywordClick(); // 성공 연출 및 UI 정리
                ImmediateExitArgument();
            }
            else
            {
                // ❌ 오답이면 카메라 셰이크 후 루프 재개
                WorngProcessKeywordClick();
                currentState = FlowState.Argument_Loop;
            }
        }
        else
        {
            WorngProcessKeywordClick();
            currentState = FlowState.Argument_Loop;
        }
        pendingLinkID = null;
    }

    // 루프가 끝나길 기다리지 않고 강제로 시스템을 정리하는 함수
    private void ImmediateExitArgument()
    {
        Debug.Log("정답 확인 - 논의 루프 즉시 탈출 및 환경 복구");

        currentState = FlowState.Dialogue_Wait;

        Time.timeScale = 1f;
        UiManager.instance.isUiAnim = false;

        UiManager.instance.camRotate(Vector3.zero, 0.5f);
        UiManager.instance.camTransform.DOMoveZ(-10f, 0.5f);
    }
    public void CancelPointOut() // "아니오" 버튼
    {
        UiManager.instance.ShowArgumentConfirmUi(false);
        pendingLinkID = null;
        currentState = FlowState.Argument_Loop; // 루프 재개
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
        if (currentEcidenceButtonSelected != null && currentEcidenceButtonSelected != button)
        {
            currentEcidenceButtonSelected.Deselect();
        }

        currentEcidenceButtonSelected = button;
    }

    public void ArgumentSelectAct(ArgumentActButton button)
    {
        activeArgumentText.text = ArgumentTextFormatter.Format(currentRawText, currentAct);

        // 이미 다른 게 선택되어 있으면 해제
        if (currentActButtonSelected != null && currentActButtonSelected != button)
        {
            currentActButtonSelected.Deselect();
        }

        currentActButtonSelected = button;

        if(currentAct == ActState.counterargument)
            currentActText.text = "반론";
        if (currentAct == ActState.agreement)
            currentActText.text = "찬성";
        if (currentAct == ActState.perjury)
            currentActText.text = "위증";
    }

    private void ClearAllEvidenceButtons()
    {
        if (argumentEvidenceButtonParent == null) return;

        foreach (Transform child in argumentEvidenceButtonParent)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in argumentActButtonParent)
        {
            child.GetComponent<ArgumentActButton>().Deselect();
        }

        currentEcidenceButtonSelected = null;
        currentActButtonSelected = null;
        currentAct = ActState.None;
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
                evidenceBtn.OnPointerClick(null);
            });
        }
    }
    // 증거 버튼 클릭 시 실행될 로직
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
    }
    #endregion

    #region Dialogue System (General)

    private void ShowDialogue(DialogueLine line)
    {
        currentState = FlowState.Dialogue_Typing;
        dialoguePanel.SetActive(true);

        // 1. 카메라 & 증거품 처리
        if (DialogueFlowManager.instance.currentPhase == DialogueFlowManager.Phase.Judgment)
            DialogueDirector.instance.TpArgumentCam(line.speaker);

        DialogueDirector.instance.ProcessEvidenceEffects(line.addEvidence, line.showEvidence);
        DialogueDirector.instance.ProcessScreenEffects(line.effect);

        // 2. UI 텍스트 설정
        DialogueDirector.instance.UpdateNameTag(line.speaker);

        // 3. (캐릭터 or 카메라) 무브 & 캐릭터 스테이트 설정
        if(line.characterPos!=Vector3.zero)
            DialogueDirector.instance.MoveCharacter(line.speaker, 1 ,line.characterPos);

        if (line.cameraPos != Vector3.zero)
            DialogueDirector.instance.MoveCamera(1, line.cameraPos);

        DialogueDirector.instance.CharacterState(line.speaker, line.charStateList);

        // 4. 캐릭터 켜기 & 끄기
        if (line.charOnList != null && line.charOnList.Count > 0)
        {
            DialogueDirector.instance.CharacterOn(line.charOnList);
        }

        if (line.charOffList != null && line.charOffList.Count > 0)
        {
            DialogueDirector.instance.CharacterOff(line.charOffList);
        }


        // 5. 배경 & BGM 업데이트
        BackgroundManager.instance.DailyMapUpdate(line.background);

        if(line.bgm=="None")
            SoundManager.instance.StopBGM();
        else if(line.bgm== "bgm_daily01")
            SoundManager.instance.BgmDaily_01();


        // 6 로그 박스 생성
        GameObject logue = Instantiate(logueBox,logueParent.transform);
        logue.GetComponent<LogueBox>().LogueBoxUpdate(line.speaker,line.text);

        // 7. 타이핑 시작
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = StartCoroutine(TypeRoutine(line.speaker, line.text, line.textTime));
    }
    IEnumerator TypeRoutine(string speaker, string text, float time)
    {
        yield return new WaitForSecondsRealtime(0.02f);

        dialogueText.text = "";
        isSkipTyping = false;
        textSlider.maxValue = 1f;
        textSlider.value = 0f;

        // 타이핑 속도 계산
        float timePerChar = Mathf.Clamp(1.0f / text.Length, 0.015f, 0.05f);

        NextImg.SetActive(false);

        for (int i = 0; i < text.Length; i++)
        {
            dialogueText.text = text.Substring(0, i + 1);
            if (i % 2 == 1 && speaker!="") SoundManager.instance.EllinaVoice(); // 보이스 재생

            yield return new WaitForSeconds(timePerChar);

            if (isSkipTyping)
            {
                dialogueText.text = text;
                break;
            }
        }

        yield return new WaitForSecondsRealtime(time);//텍스트 재생 끝나고 추가적으로 time 값 동안 기다리기

        // 타이핑 완료
        textSlider.value = 1f;
        NextImg.SetActive(true);

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

    #region Debug & Cheat System
    [Header("Debug")]
    public TMP_InputField cheatInputField; // 인스펙터에서 InputField (TMP)를 연결해 주세요.

    // 만약 기존 스크립트에 Start()가 없다면 추가해주시고, 있다면 내부 코드만 복사하세요.
    private void Start()
    {
        // 입력창에서 엔터를 눌렀을 때 (입력이 완료되었을 때) 실행될 이벤트 연결
        if (cheatInputField != null)
        {
            cheatInputField.onSubmit.AddListener(JumpToLine);
        }
    }

    public void JumpToLine(string input)
    {
        if (currentLines == null || currentLines.Count == 0)
        {
            Debug.LogWarning("[Cheat] 현재 로드된 대사(DialogueLine) 데이터가 없습니다.");
            return;
        }

        if (int.TryParse(input, out int targetIndex))
        {
            if (targetIndex >= 0 && targetIndex < currentLines.Count)
            {
                // 1. 즉시 현재 도는 모든 루프 및 타이핑 코루틴 중지
                StopAllCoroutines();

                // 🔥 [추가] 화면이 검게 굳어버리거나 피가 묻어있던 연출들을 즉시 투명하게 리셋
                if (EffectManager.instance != null)
                {
                    EffectManager.instance.ResetAllEffects();
                }
                CheatJumpWithEvidence(targetIndex);
                SynchronizeStateToLine(targetIndex);

                // 3. 각종 대기 플래그 강제 해제
                isSkipTyping = true;
                waitingExitDialogue = false;
                waitingArgumentEndText = false;
                isChoiceShowingWrongFeedback = false;
                isMapPointOutShowingWrongFeedback = false;

                // 4. UI 및 환경 초기화 (기존 씬에 남아있는 UI 찌꺼기 제거)
                ClearAllEvidenceButtons();
                dialoguePanel.SetActive(false);
                choicePanel.SetActive(false);
                argumentTextLeft.gameObject.SetActive(false);
                argumentTextRight.gameObject.SetActive(false);

                // 5. 카메라 강제 원상복구
                if (UiManager.instance != null)
                {
                    UiManager.instance.isUiAnim = false;
                    UiManager.instance.camRotate(Vector3.zero, 0f);
                }

                // 6. 새로운 인덱스 주입 및 UI 재가동
                lineIndex = targetIndex;
                dialoguePanel.SetActive(true);

                currentState = FlowState.Idle;
                PlayNext(); // 이제 동기화된 상태에서 해당 대사가 자연스럽게 나옵니다.
            }
            else
            {
                Debug.LogWarning($"[Cheat] 범위를 벗어난 인덱스입니다. (0 ~ {currentLines.Count - 1} 사이의 숫자를 입력하세요)");
            }
        }

        cheatInputField.text = "";
        cheatInputField.DeactivateInputField(); // 다음 플레이를 위해 인풋 포커스 해제
    }
    private void SynchronizeStateToLine(int targetIndex)
    {
        // 🔥 i < targetIndex (목표 대사 '직전' 까지만 연출을 덮어씌웁니다)
        for (int i = 0; i < targetIndex; i++)
        {
            DialogueLine line = currentLines[i];

            // 1. 캐릭터 On / Off (기존 DialogueDirector의 리스트 기반 함수 그대로 사용)
            DialogueDirector.instance.CharacterOn(line.charOnList);
            DialogueDirector.instance.CharacterOff(line.charOffList);

            // 2. 캐릭터 상태/표정
            if (line.charStateList.Count > 0)
            {
                DialogueDirector.instance.CharacterState(line.speaker, line.charStateList);
            }

            // 3. 배경 & BGM (보유 중인 매니저에 맞춰 연결)
            if (!string.IsNullOrEmpty(line.background))
            {
                BackgroundManager.instance.DailyMapUpdate(line.background);
            }

            if (!string.IsNullOrEmpty(line.bgm))
            {
                if (line.bgm == "None") SoundManager.instance.StopBGM();
                // else SoundManager.instance.PlayBGM(line.bgm);
            }

            // 4. 증거품 획득
            if (!string.IsNullOrEmpty(line.addEvidence))
            {
                // EvidenceManager.Instance.AddEvidence(line.addEvidence);
            }
        }

        // 5. 카메라 위치는 목표 대사의 위치로 즉시 순간이동
        if (currentLines[targetIndex].cameraPos != Vector3.zero)
        {
            Camera.main.transform.position = currentLines[targetIndex].cameraPos;
        }
    }

    public void CheatJumpWithEvidence(int targetIndex)
    {
        // 1. 인벤토리 일단 초기화 (선택 사항)
        EvidenceManager.Instance.ClearEvidence();

        // 2. 0번부터 목표 인덱스까지 루프를 돌며 증거 획득 로직만 실행
        for (int i = 0; i < targetIndex; i++)
        {
            DialogueLine line = currentLines[i];

            // 증거 획득 데이터가 있다면 실행
            if (!string.IsNullOrEmpty(line.addEvidence))
            {
                // EvidenceManager에 해당 증거를 추가 (이미 있다면 무시하도록 설계되어야 함)
                EvidenceManager.Instance.AddEvidence(line.addEvidence);
            }

            // 추가로 배경이나 BGM도 마지막 상태를 기억하고 싶다면 여기서 변수에 담아둘 수 있음
        }

        // 3. 이제 실제 대사 위치 이동 및 화면 갱신
        this.lineIndex = targetIndex;
        StopAllCoroutines();
        ResetUI();
        PlayNext();
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
        activeArgumentText.text = ArgumentTextFormatter.Format(currentRawText, currentAct);
        activeArgumentText.ForceMeshUpdate();

        if (currentLinkHoverIndex == -1) return;

        TMP_LinkInfo info = activeArgumentText.textInfo.linkInfo[currentLinkHoverIndex];

        int start = info.linkTextfirstCharacterIndex;
        int length = info.linkTextLength;

        Color hoverColor = currentAct switch
        {
            ActState.None => new Color32(77, 77, 77, 255),
            ActState.counterargument => new Color32(205, 143, 0, 255),
            ActState.agreement => new Color32(0, 207, 180, 255),
            ActState.perjury => new Color32(204, 0, 0, 255),
            _ => Color.white
        };

        for (int i = 0; i < length; i++)
        {
            int charIndex = start + i;
            var charInfo = activeArgumentText.textInfo.characterInfo[charIndex];

            if (!charInfo.isVisible) continue;

            int meshIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            var colors = activeArgumentText.textInfo.meshInfo[meshIndex].colors32;

            colors[vertexIndex + 0] = hoverColor;
            colors[vertexIndex + 1] = hoverColor;
            colors[vertexIndex + 2] = hoverColor;
            colors[vertexIndex + 3] = hoverColor;
        }

        activeArgumentText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    private void CorrectProcessKeywordClick()
    {
        if (string.IsNullOrEmpty(selectedEvidenceName))
        {
            Debug.Log("증거품을 먼저 선택하세요.");
            EffectManager.instance.CameraShake();
            return;
        }

        ArgumentBlock block = argumentBlocks[currentBlockIndex];

        // 🔥 1. 증거 일치 여부
        bool isEvidenceCorrect = (selectedEvidenceName == correctEvidenceName);

        // 🔥 2. 행동(찬성/반론) 일치 여부
        bool isActCorrect = (currentAct == block.actionType);

        if (isEvidenceCorrect && isActCorrect)
        {
            Debug.Log("이의 있음! (정답)");
            StartCoroutine(CorrectAnswer(2.2f));
            ArgumentCorrect(); // 내부에서 currentState를 Dialogue_Wait 등으로 전환
        }
        else
        {
            Debug.Log("틀렸습니다. (증거품 또는 행동 불일치)");
            EffectManager.instance.CameraShake();
        }
    }

    private void WorngProcessKeywordClick()
    {
        Debug.Log("틀렸습니다. (틀린 키워드 클릭)");
        EffectManager.instance.CameraShake();
    }

    #endregion

    #region Helper Methods (Camera, Effect, Data)

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
        currentEcidenceButtonSelected = null;
        currentActButtonSelected = null;
        currentAct = ActState.None;
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
        SoundManager.instance.UiSelect();

        if (choiceIndex == line.correctIndex)
        {
            Debug.Log("정답!");
            StartCoroutine(CorrectAnswer(2.2f));
            isChoiceShowingWrongFeedback = false;
            choicePanel.SetActive(false);
            PlayNext();
        }
        else
        {
            Debug.Log("오답!");
            EffectManager.instance.CameraShake();

            // 플래그 설정: 다음 클릭 시 PlayNext()가 아닌 선택지로 돌아가게 함
            isChoiceShowingWrongFeedback = true;

            // 대화창이 뜰 때 선택지 버튼이 가려지도록 잠시 비활성화 (선택 사항)
            choicePanel.SetActive(false);

            DialogueLine wrongLine = new DialogueLine
            {
                speaker = "엘리나",
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
        
        UiManager.instance.MapPointOutUiToggle();

        //3. 다이얼로그 숨기기
        dialoguePanel.SetActive(false);
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
            StartCoroutine(CorrectAnswer(2.2f));

            UiManager.instance.MapPointOutUiToggle();

            isMapPointOutShowingWrongFeedback = false;
        }
        else
        {
            Debug.Log("오답!");
            EffectManager.instance.CameraShake();

            isMapPointOutShowingWrongFeedback = true;

            DialogueLine wrongLine = new DialogueLine
            {
                speaker = "엘리나",
                text = "(여기는 아닌가봐... 다시 한번 생각해보자.)",
                type = DialogueType.Dialogue
            };

            ShowDialogue(wrongLine);
        }
    }

    /*// UI 연출이 끝난 뒤 대화를 이어가기 위한 헬퍼 코루틴
    private IEnumerator WaitAndPlayNext(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        PlayNext();
    }
    */
    #endregion

    IEnumerator CorrectAnswer(float waitTime)
    {
        currentState = FlowState.Idle;
        isObjectionAnim = true;
        EffectManager.instance.CameraShake();
        UiManager.instance.HanlonAnimOn(currentAct);
        yield return new WaitForSecondsRealtime(waitTime);
        currentState = FlowState.Dialogue_Wait;
        yield return new WaitForSecondsRealtime(1.2f);
        isObjectionAnim = false;
        PlayNext();
    }
}