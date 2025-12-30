using KoreanTyper;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text nameText;
    public TMP_Text dialogueText;
    public Slider textSlider;

    public ArgumentManager argumentManager;

    private List<DialogueLine> lines;
    private int index = 0;
    private bool isActive = false;
    private Coroutine typingRoutine;

    public void StartDialogue(List<DialogueLine> dialogue)
    {
        lines = dialogue;
        index = 0;
        isActive = true;
        ShowLine();
    }

    private void ShowLine()
    {
        if (index >= lines.Count)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = lines[index];

        // ① 심문 시작
        if (line.type == DialogueType.ArgumentStart)
        {
            List<DialogueLine> block = CollectArgumentBlock();
            argumentManager.StartArgument(block, OnArgumentFinished);
            return;
        }

        // ② 심문 중 줄은 DialogueManager에서 출력 X
        if (line.type == DialogueType.Argument)
        {
            index++;
            ShowLine();
            return;
        }

        // ③ 일반 대사
        nameText.text = line.speaker;

        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        typingRoutine = StartCoroutine(TypeCoroutine(line.text.Typing(textSlider.value)));
    }

    IEnumerator TypeCoroutine(string text)
    {
        int len = text.Length;
        textSlider.maxValue = 1;
        textSlider.value = 0;

        string curr = "";

        for (int i = 0; i < len; i++)
        {
            curr = text.Substring(0, i + 1);
            dialogueText.text = curr;

            yield return new WaitForSeconds(0.03f);
        }

        textSlider.value = 1;
    }

    public void NextLine()
    {
        index++;
        ShowLine();
    }

    private void EndDialogue()
    {
        isActive = false;
        dialogueText.text = "";
        nameText.text = "";
        Debug.Log("일반 대사 종료");
    }

    // ● 심문 블록 수집
    private List<DialogueLine> CollectArgumentBlock()
    {
        List<DialogueLine> block = new List<DialogueLine>();

        index++; // "심문 시작" 다음 줄부터

        while (index < lines.Count && lines[index].type != DialogueType.ArgumentEnd)
        {
            if (lines[index].type == DialogueType.Argument)
                block.Add(lines[index]);

            index++;
        }

        index++; // "심문 종료" 넘기기

        return block;
    }

    // ● 심문 종료 후 이어서 일반 대사 진행
    private void OnArgumentFinished()
    {
        Debug.Log("심문 종료 → Dialogue 복귀");
        ShowLine();
    }
}

