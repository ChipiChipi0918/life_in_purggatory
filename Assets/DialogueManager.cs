using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text nameText;
    public TMP_Text dialogueText;

    private List<DialogueLine> lines;
    private int index = 0;
    private bool isActive = false;

    void Update()
    {
        if (!isActive) return;

        // 클릭 또는 터치 시 대사 넘김
        if (Input.GetMouseButtonDown(0))
        {
            NextLine();
        }
    }

    // 외부에서 대사 리스트를 받아 시작
    public void StartDialogue(List<DialogueLine> dialogue)
    {
        if (dialogue == null || dialogue.Count == 0)
        {
            Debug.LogWarning("대사 없음");
            return;
        }

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

        nameText.text = line.speaker;
        dialogueText.text = line.text;
    }

    private void NextLine()
    {
        index++;
        ShowLine();
    }

    private void EndDialogue()
    {
        isActive = false;
        nameText.text = "";
        dialogueText.text = "";
        Debug.Log("대사 종료");
    }
}
