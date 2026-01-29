using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class DialogueFlowManager : MonoBehaviour
{
    public static DialogueFlowManager instance;

    public GoogleSheetLoader loader;
    public ArgumentManager argumentManager;

    private List<DialogueLine> allLines = new List<DialogueLine>();

    public enum Phase
    {
        Daily,
        Judgment,
        End
    }

    public Phase currentPhase;

    IEnumerator Start()
    {
        argumentManager.OnAllDialogueFinished += OnDialogueFinished;

        yield return LoadCurrentPhase();
        argumentManager.PlayLines(allLines);
    }

    IEnumerator LoadCurrentPhase()
    {
        allLines.Clear(); // 🔥 항상 초기화

        if (currentPhase == Phase.Daily)
        {
            yield return loader.LoadDialogueCSV(OnLoaded);
        }
        else if (currentPhase == Phase.Judgment)
        {
            yield return loader.LoadArgumentCSV(OnLoaded);
        }
    }

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    void OnLoaded(List<DialogueLine> lines)
    {
        if (lines != null)
            allLines.AddRange(lines);
    }

    void OnDialogueFinished()
    {
        if (currentPhase == Phase.Daily)
        {
            currentPhase = Phase.Judgment;
            BackgroundManager.instance.ChangeNomalToTribunal();
            StartCoroutine(StartNextPhase());
        }
        else if (currentPhase == Phase.Judgment)
        {
            Debug.Log("모든 파트 종료");
        }
    }


    IEnumerator StartNextPhase()
    {
        yield return LoadCurrentPhase();
        argumentManager.PlayLines(allLines);
    }
}
