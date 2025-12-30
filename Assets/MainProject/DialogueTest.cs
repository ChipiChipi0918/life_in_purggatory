using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueTest : MonoBehaviour
{
    public GoogleSheetLoader loader;
    public DialogueManager dialogueManager;
    public ArgumentManager argumentManager;

    IEnumerator Start()
    {
        yield return loader.LoadDialogueCSV(OnDialogueLoaded);
    }

    void OnDialogueLoaded(List<DialogueLine> lines)
    {
        if (lines == null) return;

        dialogueManager.argumentManager = argumentManager;
        dialogueManager.StartDialogue(lines);
    }

}
