using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueTest : MonoBehaviour
{
    public bool isDaily = true;

    public GoogleSheetLoader loader;
    public ArgumentManager argumentManager;

    private List<DialogueLine> allLines = new List<DialogueLine>();

    IEnumerator Start()
    {
        if(isDaily)
            yield return loader.LoadDialogueCSV(OnDialogueLoaded);

        else 
            yield return loader.LoadArgumentCSV(OnArgumentLoaded);

        argumentManager.PlayLines(allLines);
    }

    void OnDialogueLoaded(List<DialogueLine> lines)
    {
        if (lines != null)
            allLines.AddRange(lines);
    }

    void OnArgumentLoaded(List<DialogueLine> lines)
    {
        if (lines != null)
            allLines.AddRange(lines);
    }
}
