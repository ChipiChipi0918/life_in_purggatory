using DG.Tweening;
using KoreanTyper;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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


    [Header("Argument")]
    public TMP_Text argumentText;
    public CanvasGroup argumentTextCancasGroup;
    public float argumentDuration = 0.5f;
    public Transform argumentCamTransform;
    private string beforeSpeaker;
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

    private void MoveCam(string name)
    {
        if (name == "엘리나") argumentCamTransform.DOMoveX(0, 0.5f);
        else if (name == "헤스터") argumentCamTransform.DOMoveX(-40, 0.5f);
        else if (name == "미르엘") argumentCamTransform.DOMoveX(-20, 0.5f);
        else if (name == "알베르트") argumentCamTransform.DOMoveX(40, 0.5f);
        else if (name == "루카스") argumentCamTransform.DOMoveX(20, 0.5f);
    }
    private void TpCam(string name)
    {
        if (name == "엘리나") argumentCamTransform.position = new Vector3(0-2, 0,-10);
        else if (name == "헤스터") argumentCamTransform.position = new Vector3(-40-2, 0, -10);
        else if (name == "미르엘") argumentCamTransform.position = new Vector3(-20-2, 0,-10);
        else if (name == "알베르트") argumentCamTransform.position = new Vector3(40-2, 0, -10);
        else if (name == "루카스") argumentCamTransform.position = new Vector3(20-2, 0,-10);
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

        MoveCam(line.speaker);

        if (argumentRoutine != null)
            StopCoroutine(argumentRoutine);

        argumentRoutine = StartCoroutine(PlayArgument(line));
    }

    IEnumerator PlayArgument(DialogueLine line)
    {
        rawText = line.text;
        argumentText.text = ArgumentTextFormatter.Format(rawText);

        argumentTextCancasGroup.alpha = 0f;
        argumentTextCancasGroup.interactable = false;
        argumentTextCancasGroup.blocksRaycasts = false;

        if (beforeSpeaker == line.speaker)
            yield return new WaitForSeconds(0.2f);
        else
            yield return new WaitForSeconds(0.5f);

        float time = 0f;

        while (time < argumentDuration)
        {
            time += Time.deltaTime;
            argumentTextCancasGroup.alpha = Mathf.Lerp(0f, 1f, time / argumentDuration);
            yield return null;
        }

        argumentTextCancasGroup.alpha = 1f;
        argumentTextCancasGroup.interactable = true;
        argumentTextCancasGroup.blocksRaycasts = true;

        yield return new WaitForSeconds(line.duration);

        beforeSpeaker = line.speaker;

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

            GameObject btnObj = Instantiate(choiceButtonPrefab , choiceButtonParent);
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

        UiManager.instance.camRotate(new Vector3(0, 0, 0f));

        SkipToAfterExitLine();
    }


    private void ShowDialogue(DialogueLine line)
    {

        argumentText.text = "";

        // --- 이름 ---
        nameImg.SetActive(line.speaker != "");
        nameText.text = line.speaker;

        TpCam(line.speaker);

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
            if (i % 2 == 0) VoiceSoundPlay(name);
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
                TpCam("엘리나");
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
        if (argumentText.text == "") return;

        Camera cam = null;
        if (argumentText.canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = argumentText.canvas.worldCamera;

        int link = TMP_TextUtilities.FindIntersectingLink(argumentText, Input.mousePosition, cam);

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
            argumentText.text = ArgumentTextFormatter.Format(rawText);
            return;
        }

        TMP_LinkInfo info = argumentText.textInfo.linkInfo[hoverIndex];
        string id = info.GetLinkID();
        if (!id.StartsWith("cmd:")) return;

        string keyword = id.Substring(4);

        string hovered =
            $"<link=\"cmd:{keyword}\"><b><color=#CC0000>{keyword}</color></b></link>";

        string normal =
            $"<link=\"cmd:{keyword}\"><b><color=#FF4444>{keyword}</color></b></link>";

        string formatted = ArgumentTextFormatter.Format(rawText);
        argumentText.text = formatted.Replace(normal, hovered);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Camera cam = null;
        if (argumentText.canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = argumentText.canvas.worldCamera;

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(argumentText, eventData.position, cam);
        if (linkIndex == -1) return;

        TMP_LinkInfo linkInfo = argumentText.textInfo.linkInfo[linkIndex];
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
        argumentText.text = "";
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
