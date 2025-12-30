using System;
using KoreanTyper;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

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

            string target = text.Substring(start + 1, end - start - 1);

            string replacement =
                $"<link=\"cmd:{target}\"><b><color=#FF4444>{target}</color></b></link>";

            text = text.Remove(start, end - start + 1)
                       .Insert(start, replacement);
        }

        return text;
    }
}

public class ArgumentManager : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    public TMP_Text argumentText;

    private List<DialogueLine> lines;
    private int index = 0;
    private bool isActive = false;

    private string rawText = "";
    private int hoverIndex = -1;
    private Action onFinished;


    public void StartArgument(List<DialogueLine> dialogue, Action finishCallback)
    {
        lines = dialogue;
        index = 0;
        isActive = true;
        onFinished = finishCallback;

        StartCoroutine(PlayArgument());
    }

    private IEnumerator PlayArgument()
    {
        while (isActive && index < lines.Count)
        {
            DialogueLine line = lines[index];

            rawText = line.text;
            argumentText.text = ArgumentTextFormatter.Format(rawText);

            yield return new WaitForSeconds(line.duration);

            index++;
        }

        EndArgument();
    }

    private void EndArgument()
    {
        isActive = false;
        argumentText.text = "";

        Debug.Log("어그리먼트 종료");

        onFinished?.Invoke();
    }

    // (Hover / Click 로직 기존 그대로)
    public void OnPointerClick(PointerEventData eventData)
    {
        Camera cam = null;

        if (argumentText.canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = argumentText.canvas.worldCamera;

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(argumentText,
            eventData.position, cam);

        if (linkIndex == -1) return;

        TMP_LinkInfo linkInfo = argumentText.textInfo.linkInfo[linkIndex];

        string id = linkInfo.GetLinkID();

        if (id.StartsWith("cmd:"))
        {
            string keyword = id.Substring(4);
            Debug.Log("클릭한 강조 문구: " + keyword);
        }
    }

    private void Update()
    {
        if (!isActive) return;
        CheckHover();
    }

    private void CheckHover()
    {
        Camera cam = null;
        if (argumentText.canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = argumentText.canvas.worldCamera;

        int link = TMP_TextUtilities.FindIntersectingLink(argumentText,
            Input.mousePosition, cam);

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

}
