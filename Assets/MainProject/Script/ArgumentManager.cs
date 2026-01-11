using DG.Tweening;
using KoreanTyper;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;
using static Unity.Collections.AllocatorManager;

public static class ArgumentTextFormatter
{
    public static string Format(string text)
    {
        while (true)
        {
            int start = text.IndexOf('*');
            if (start == -1) break;

            int end = text.IndexOf('*', start + 1);
            if (end == -1) break;

            string keyword = text.Substring(start + 1, end - start - 1);

            string replacement =
                $"<link=\"cmd:{keyword}\"><b><color=#FF4444>{keyword}</color></b></link>";

            text = text.Remove(start, end - start + 1)
                       .Insert(start, replacement);
        }

        return text;
    }
}

public class ArgumentManager : MonoBehaviour, IPointerClickHandler
{
    public static ArgumentManager instance;

    private bool _isArgumentActive = false;
    public bool isArgumentActive
    {
        get => _isArgumentActive;
        set => _isArgumentActive = value; // ⚡ UI 제어 제거
    }

    public static List<ArgumentBlock> argumentBlocks;

    private int currentBlockIndex = 0;
    private int repeatIndex = 0;
    private bool waitingExitDialogue = false;
    private bool argumentForceEnded = false;
    private bool waitingArgumentEndText = false;

    [Header("Choice")]
    public GameObject choicePanel;      // 선택지 전체 패널
    public GameObject choiceButtonPrefab; // 버튼 프리팹
    public Transform choiceButtonParent;  // 버튼 배치될 위치

    [Header("Argument Text")]
    public TMP_Text argumentText1;
    public TMP_Text argumentText2;
    private TMP_Text nowArgumentText;

    [Header("Argument")]
    public float argumentDuration = 0.5f;
    public Transform argumentCamTransform;
    private string beforeSpeaker;
    private string beforeCamFormat;
    private bool isChoice;

    [Header("Dialogue")]
    public GameObject dialogue;
    public GameObject nameImg;
    public TMP_Text nameText;
    public TMP_Text dialogueText;
    public Slider textSlider;

    private List<DialogueLine> lines;
    private int index = 0;

    private Coroutine argumentRoutine;
    private Coroutine typingRoutine;

    private string rawText = "";
    private int hoverIndex = -1;

    private bool lastUiAnim = false;
    private bool shouldDialogueBeActive = false;


    private void Awake()
    {
        if (instance == null) instance = this;

        nowArgumentText = argumentText1;

        argumentText1.gameObject.SetActive(false);
        argumentText2.gameObject.SetActive(false);
    }

    #region Public Entry
    public void PlayLines(List<DialogueLine> dialogueLines)
    {
        if (dialogueLines == null || dialogueLines.Count == 0)
        {
            Debug.LogWarning("대사 데이터 없음");
            return;
        }

        lines = dialogueLines;
        index = 0;

        StopAllCoroutines();
        ClearUI();

        PlayNext();
    }
    #endregion

    #region Core Flow
    private void PlayNext()
    {
        if (UiManager.instance != null && UiManager.instance.isUiAnim)
            return;

        // 🔥 종료 후 대사 출력이 끝나고 유저가 클릭하면 다시 논의 시작
        if (waitingExitDialogue)
        {

            if (textSlider.value == 1) // 대사 타이핑 끝났고 → 클릭한 순간
            {
                waitingExitDialogue = false;
                isArgumentActive = true;       // 다시 논의 모드 시작
                UiManager.instance.ArgumentUiOn(true);

                PlayArgumentLine();            // 0번 주장부터 재시작
            }
            return;
        }

        // 🔥 논의 반복 상태라면 계속 진행
        if (isArgumentActive)
        {
            PlayArgumentLine();
            return;
        }

        // 🔥 일반 Dialogue 진행
        if (index >= lines.Count)
        {
            EndAll();
            return;
        }

        DialogueLine line = lines[index];
        index++;

        if (line.type == DialogueType.Argument)
        {
            if (argumentForceEnded)
            {
                PlayNext();
                return;
            }

            isArgumentActive = true;
            UiManager.instance.ArgumentUiOn(true);

            PlayArgumentLine();
        }
        else
        {
            // 🔥 선택지 처리 추가
            if (line.isChoice)
            {
                ShowChoice(line);
            }
            else
            {
                ShowDialogue(line);
            }
        }

    }
    private void ArgumentTextUpdate(DialogueLine line)
    {
        argumentText1.gameObject.SetActive(false);
        argumentText2.gameObject.SetActive(false);

        if (line.camFormat == "left")
        {
            nowArgumentText = argumentText1;
            MoveCam(line.speaker, 2.2f);

            if (repeatIndex > 1 && repeatIndex != argumentBlocks[currentBlockIndex].lines.Count)
                UiManager.instance.camRotate(new Vector3(0, 0, 2.5f), 0.5f);
        }
        else if (line.camFormat == "right")
        {
            nowArgumentText = argumentText2;
            MoveCam(line.speaker, -2.2f);

            if(repeatIndex > 1 && repeatIndex != argumentBlocks[currentBlockIndex].lines.Count)
                UiManager.instance.camRotate(new Vector3(0, 0, -2.5f),0.5f);
        }

        nowArgumentText.gameObject.SetActive(true);
    }

    public void MoveCam(string name,float addPosX)
    {
        float time = 0.5f;
        if (name == "주인공")      argumentCamTransform.DOMoveX(0 + addPosX, time);
        else if (name == "백현")   argumentCamTransform.DOMoveX(-80 + addPosX, time);
        else if (name == "친구")   argumentCamTransform.DOMoveX(-60 + addPosX, time);
        else if (name == "다니엘") argumentCamTransform.DOMoveX(-40 + addPosX, time);
        else if (name == "정희영") argumentCamTransform.DOMoveX(-20 + addPosX, time);
        else if (name == "장현우") argumentCamTransform.DOMoveX(20 + addPosX, time);
        else if (name == "천주연") argumentCamTransform.DOMoveX(40 + addPosX, time);
        else if (name == "정태준") argumentCamTransform.DOMoveX(60 + addPosX, time);
        else if (name == "서진랑") argumentCamTransform.DOMoveX(80 + addPosX, time);
    }
    public void TpCam(string name)
    {
        if (name == "주인공")      argumentCamTransform.position = new Vector3(0, 0, -10);
        else if (name == "백현")   argumentCamTransform.position = new Vector3(-80, 0, -10);
        else if (name == "친구")   argumentCamTransform.position = new Vector3(-60, 0, -10);
        else if (name == "다니엘") argumentCamTransform.position = new Vector3(-40, 0, -10);
        else if (name == "정희영") argumentCamTransform.position = new Vector3(-20, 0, -10);
        else if (name == "장현우") argumentCamTransform.position = new Vector3(20, 0, -10);
        else if (name == "천주연") argumentCamTransform.position = new Vector3(40, 0, -10);
        else if (name == "정태준") argumentCamTransform.position = new Vector3(60, 0, -10);
        else if (name == "서진랑") argumentCamTransform.position = new Vector3(80, 0, -10);
    }
    private void PlayArgumentLine()
    {
        
        dialogue.SetActive(false);

        ArgumentBlock block = argumentBlocks[currentBlockIndex];

        // 🔥 반복 끝 → 종료 후 대사 출력
        if (repeatIndex >= block.lines.Count)
        {
            isArgumentActive = false;
            waitingExitDialogue = true;

            UiManager.instance.ArgumentUiOn(false);
            repeatIndex = 0;

            // 🔥 자연 종료일 때만 exitLine 출력
            if (!argumentForceEnded)
            {
                ShowDialogue(block.exitLine);
                waitingArgumentEndText = true;   // 🔥 exitLine 출력 후 Hello World 단계로 진입
            }

            return;
        }

        // 🔥 반복 중
        DialogueLine line = block.lines[repeatIndex];
        repeatIndex++;

        ArgumentTextUpdate(line);

        if (argumentRoutine != null)
            StopCoroutine(argumentRoutine);

        argumentRoutine = StartCoroutine(PlayArgument(line));
    }

    IEnumerator PlayArgument(DialogueLine line)
    {
        rawText = line.text;
        nowArgumentText.text = ArgumentTextFormatter.Format(rawText);

        CanvasGroup argumentTextCanvasGroup = nowArgumentText.GetComponent<CanvasGroup>();

        argumentTextCanvasGroup.alpha = 0f;
        argumentTextCanvasGroup.interactable = false;
        argumentTextCanvasGroup.blocksRaycasts = false;

        if (beforeSpeaker == line.speaker && beforeCamFormat == line.camFormat)
            yield return new WaitForSeconds(0.2f);
        else
            yield return new WaitForSeconds(0.5f);

        float time = 0f;

        while (time < argumentDuration)
        {
            time += Time.deltaTime;

            argumentTextCanvasGroup.alpha = Mathf.Lerp(0f, 1f, time / argumentDuration);
            yield return null;
        }

        argumentTextCanvasGroup.alpha = 1f;
        argumentTextCanvasGroup.interactable = true;
        argumentTextCanvasGroup.blocksRaycasts = true;

        yield return new WaitForSeconds(line.duration);

        beforeSpeaker = line.speaker;
        beforeCamFormat = line.camFormat;

        PlayNext();
    }
    private void ShowChoice(DialogueLine line)
    {
        isChoice = true;
        Debug.Log("선택지 발견!");

        choicePanel.SetActive(true);

        // 기존 버튼들 삭제
        foreach (Transform child in choiceButtonParent)
            Destroy(child.gameObject);

        // 선택지 생성
        for (int i = 0; i < line.choices.Count; i++)
        {
            int index = i;

            GameObject btnObj = Instantiate(choiceButtonPrefab, choiceButtonParent);
            btnObj.transform.localPosition += new Vector3(i * 50, i * 100);
            TMP_Text btnText = btnObj.transform.GetChild(0).GetComponent<TMP_Text>();
            btnText.text = line.choices[i];

            btnObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                OnChoiceSelected(line, index);
            });
        }
    }

    private void OnChoiceSelected(DialogueLine line, int choice)
    {
        // 정답
        if (choice == line.correctIndex)
        {
            Debug.Log("정답 선택!");

            isChoice = false;

            choicePanel.SetActive(false);

            // 다음 대사로 진행
            PlayNext();
        }
        else
        {
            Debug.Log("오답 선택!");

            // 지금은 오답이어도 그냥 다시 고르게 놔둠
            UiManager.instance.Shaking(0.5f);
            line.text = "(역시... 이건 아닌가봐... 다시 한번 생각해보자)";
            ShowDialogue(line);
        }
    }


    public void ForceEndArgumentLoop()
    {
        ArgumentBlock block = ArgumentManager.argumentBlocks[currentBlockIndex];

        // 반복 인덱스를 끝까지 밀기
        repeatIndex = block.lines.Count;

        // 논의 상태 종료 플래그
        isArgumentActive = false;
        waitingExitDialogue = true;

        // UI 닫기
        UiManager.instance.ArgumentUiOn(false);

        // 종료 후 출력될 대사 즉시 재생
        ShowDialogue(block.exitLine);
    }
    private void SkipToAfterExitLine()
    {
        if (lines == null || lines.Count == 0) return;

        DialogueLine exitLine = ArgumentManager.argumentBlocks[currentBlockIndex].exitLine;

        int exitIndex = lines.FindIndex(l =>
            l.speaker == exitLine.speaker &&
            l.text == exitLine.text);

        if (exitIndex != -1)
            index = exitIndex + 1;
    }

    private void ArgumentCorrect()
    {
        argumentForceEnded = true;
        repeatIndex = 0;
        isArgumentActive = false;
        waitingExitDialogue = false;

        UiManager.instance.camRotate(new Vector3(0, 0, 0f), 1);

        SkipToAfterExitLine();
    }


    private void ShowDialogue(DialogueLine line)
    {
        TpCam(line.speaker); //말하는 사람으로 카메라 이동
        nowArgumentText.text = "";

        // 🔥 증거 추가 처리
        if (!string.IsNullOrEmpty(line.addEvidence) && EvidenceManager.Instance != null)
        {
            EvidenceManager.Instance.AddEvidence(line.addEvidence);
        }


        // --- 이름 ---
        nameImg.SetActive(line.speaker != "");
        string result = "이름";

        if (line.speaker == "주인공") result = $"<size=180%><color=#FFE2A0>{line.speaker[0]}</color></size>{line.speaker.Substring(1)}";
        else if (line.speaker == "백현") result = $"<size=180%><color=#0E432D>{line.speaker[0]}</color></size>{line.speaker.Substring(1)}";
        else if (line.speaker == "친구") result = $"<size=180%><color=#260001>{line.speaker[0]}</color></size>{line.speaker.Substring(1)}";
        else if (line.speaker == "다니엘") result = $"<size=180%><color=#E7A300>{line.speaker[0]}</color></size>{line.speaker.Substring(1)}";
        else if (line.speaker == "정희영") result = $"<size=180%><color=#9B2BFF>{line.speaker[0]}</color></size>{line.speaker.Substring(1)}";
        else if (line.speaker == "장현우") result = $"<size=180%><color=#CB1B00>{line.speaker[0]}</color></size>{line.speaker.Substring(1)}";
        else if (line.speaker == "천주연") result = $"<size=180%><color=#FFE945>{line.speaker[0]}</color></size>{line.speaker.Substring(1)}";
        else if (line.speaker == "정태준") result = $"<size=180%><color=#1572FF>{line.speaker[0]}</color></size>{line.speaker.Substring(1)}";
        else if (line.speaker == "서진랑") result = $"<size=180%><color=#5E3200>{line.speaker[0]}</color></size>{line.speaker.Substring(1)}";

        nameText.text = result;



        // --- dialogue 활성화 관리 ---
        if (UiManager.instance.isUiAnim)
        {
            shouldDialogueBeActive = true;
        }
        else
        {
            dialogue.SetActive(true);
            shouldDialogueBeActive = false;
        }

        // 타이핑
        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        typingRoutine = StartCoroutine(TypeCoroutine(line.speaker, line.text));
    }

    #endregion

    #region Typing Effect
    IEnumerator TypeCoroutine(string name, string text)
    {
        int length = text.Length;

        textSlider.maxValue = 1f;
        textSlider.value = 0f;

        float totalTime = 1.0f;
        float timePerChar = totalTime / length;
        if (timePerChar < 0.015f) timePerChar = 0.015f;
        else if (timePerChar > 0.05f) timePerChar = 0.05f;

        for (int i = 0; i < length; i++)
        {
            dialogueText.text = text.Substring(0, i + 1);
            if (i % 2 == 1) VoiceSoundPlay(name);
            yield return new WaitForSeconds(timePerChar);
        }
        yield return new WaitForSeconds(0.225f);

        textSlider.value = 1f;
    }

    private void VoiceSoundPlay(string name)
    {
        if (name == "") return;

        if (name == "엘리나") SoundManager.instance.ElinaVoice();
        else SoundManager.instance.ElinaVoice();
    }
    #endregion

    #region Update & Input
    private void Update()
    {
        if (lines == null) return;

        // 🔥 UI 애니메이션 끝난 순간
        if (lastUiAnim == true && UiManager.instance.isUiAnim == false)
        {
            if (shouldDialogueBeActive)
            {
                dialogue.SetActive(true);
                shouldDialogueBeActive = false;
            }
        }

        
        lastUiAnim = UiManager.instance.isUiAnim;

        if (Input.GetMouseButtonDown(0) && isArgumentActive == false && isChoice == false && textSlider.value == 1)
        {
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
                return;
            }

            if (waitingExitDialogue)
            {
                waitingExitDialogue = false;
                isArgumentActive = true;
                UiManager.instance.ArgumentUiOn(true);

                PlayArgumentLine();
                return;
            }

            // 3) 기본적인 흐름
            PlayNext();
        }
        CheckHover();
    }
    #endregion

    #region Hover & Click

    private void CheckHover()
    {
        if (nowArgumentText.text == "") return;

        Camera cam = null;
        if (nowArgumentText.canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = nowArgumentText.canvas.worldCamera;

        int link = TMP_TextUtilities.FindIntersectingLink(nowArgumentText, Input.mousePosition, cam);

        if (link != hoverIndex)
        {
            hoverIndex = link;
            ApplyHoverEffect();
        }
    }

    private void ApplyHoverEffect()
    {
        if (hoverIndex == -1)
        {
            nowArgumentText.text = ArgumentTextFormatter.Format(rawText);
            return;
        }

        TMP_LinkInfo info = nowArgumentText.textInfo.linkInfo[hoverIndex];
        string id = info.GetLinkID();
        if (!id.StartsWith("cmd:")) return;

        string keyword = id.Substring(4);

        string hovered =
            $"<link=\"cmd:{keyword}\"><b><color=#CC0000>{keyword}</color></b></link>";

        string normal =
            $"<link=\"cmd:{keyword}\"><b><color=#FF4444>{keyword}</color></b></link>";

        string formatted = ArgumentTextFormatter.Format(rawText);
        nowArgumentText.text = formatted.Replace(normal, hovered);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Camera cam = null;
        if (nowArgumentText.canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = nowArgumentText.canvas.worldCamera;

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(nowArgumentText, eventData.position, cam);
        if (linkIndex == -1) return;

        TMP_LinkInfo linkInfo = nowArgumentText.textInfo.linkInfo[linkIndex];
        string id = linkInfo.GetLinkID();

        if (id.StartsWith("cmd:"))
        {
            string keyword = id.Substring(4);
            Debug.Log("클릭된 키워드: " + keyword);

            // ⬇ 논의 종료 처리 (exitLine 스킵)
            ArgumentCorrect();
        }
    }

    #endregion

    #region Utils
    private void ClearUI()
    {
        nowArgumentText.text = "";
        nameText.text = "";
        dialogueText.text = "";
        textSlider.value = 0f;
    }

    private void EndAll()
    {
        ClearUI();
        Debug.Log("전체 대사 종료");
    }
    #endregion
}
