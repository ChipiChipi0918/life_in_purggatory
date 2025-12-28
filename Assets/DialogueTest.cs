using System.Collections.Generic;
using UnityEngine;

public class DialogueTest : MonoBehaviour
{
    public GoogleSheetLoader loader;
    public DialogueManager manager;

    private void Start()
    {
        StartCoroutine(loader.LoadDialogue(OnLoaded));
    }

    void OnLoaded(List<DialogueLine> lines)
    {
        if (lines == null) return;

        manager.StartDialogue(lines);
    }
}
