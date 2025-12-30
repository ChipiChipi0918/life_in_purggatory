using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using KoreanTyper;

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
    public bool isArgumentActive = false;

    [Header("Argument UI")]
    public TMP_Text argumentText;

    [Header("Dialogue UI")]
    public TMP_Text nameText;
    public TMP_Text dialogueText;
    public Slider textSlider;

    private List<DialogueLine> lines;
    private int index = 0;

    private Coroutine argumentRoutine;
    private Coroutine typingRoutine;

    private string rawText = "";
    private int hoverIndex = -1;

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
        if (index >= lines.Count)
        {
            isArgumentActive = false;
            EndAll();
            return;
        }

        DialogueLine line = lines[index];

        isArgumentActive = (line.type == DialogueType.Argument);

        if (isArgumentActive)
        {
            if (argumentRoutine != null)
                StopCoroutine(argumentRoutine);

            argumentRoutine = StartCoroutine(PlayArgument(line));
        }
        else
        {
            ShowDialogue(line);
        }

        index++;
    }


    IEnumerator PlayArgument(DialogueLine line)
    {
        rawText = line.text;
        argumentText.text = ArgumentTextFormatter.Format(rawText);

        yield return new WaitForSeconds(line.duration);

        PlayNext();
    }


    private void ShowDialogue(DialogueLine line)
    {
        argumentText.text = "";

        nameText.text = line.speaker;

        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        typingRoutine = StartCoroutine(TypeCoroutine(line.text));
    }

    #endregion

    #region Typing Effect

    IEnumerator TypeCoroutine(string text)
    {
        int length = text.Length;

        textSlider.maxValue = 1f;
        textSlider.value = 0f;

        float totalTime = 1.0f;
        float timePerChar = totalTime / length;

        for (int i = 0; i < length; i++)
        {
            dialogueText.text = text.Substring(0, i + 1);
            yield return new WaitForSeconds(timePerChar);
        }

        textSlider.value = 1f;
    }

    #endregion

    #region Update & Input

    private void Update()
    {
        if (lines == null) return;

        if (Input.GetMouseButtonDown(0) && isArgumentActive==false && textSlider.value==1)
        {
            PlayNext();
        }

        CheckHover();
    }

    #endregion

    #region Hover & Click (Argument)

    private void CheckHover()
    {
        if (argumentText.text == "") return;

        Camera cam = null;
        if (argumentText.canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = argumentText.canvas.worldCamera;

        int link = TMP_TextUtilities.FindIntersectingLink(
            argumentText, Input.mousePosition, cam);

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

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(
            argumentText, eventData.position, cam);

        if (linkIndex == -1) return;

        TMP_LinkInfo linkInfo = argumentText.textInfo.linkInfo[linkIndex];
        string id = linkInfo.GetLinkID();

        if (id.StartsWith("cmd:"))
        {
            string keyword = id.Substring(4);
            Debug.Log("클릭된 키워드: " + keyword);
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
