using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameFlowController : MonoBehaviour
{
    public DialogueManager dialogueManager;
    public ArgumentManager argumentManager;

    private List<DialogueLine> lines;
    private int index = 0;

    public void StartFlow(List<DialogueLine> dialogue)
    {
        lines = dialogue;
        index = 0;

        Next();
    }

    private void Next()
    {
        if (index >= lines.Count) return;

        DialogueLine line = lines[index];

        // ① 심문 시작
        if (line.speaker == "심문 시작")
        {
            List<DialogueLine> arguList = new List<DialogueLine>();

            index++;
            while (index < lines.Count && lines[index].speaker != "심문 종료")
            {
                arguList.Add(lines[index]);
                index++;
            }

            // "심문 종료" 스킵
            index++;

            argumentManager.StartArgument(arguList, Next);
            return;
        }

        // ② 일반 대사
        dialogueManager.StartDialogue(new List<DialogueLine>() { line });

        index++;
    }
}
