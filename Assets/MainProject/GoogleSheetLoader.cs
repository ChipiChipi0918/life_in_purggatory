using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
public enum DialogueType
{
    Normal,
    ArgumentStart,
    Argument,
    ArgumentEnd
}


[System.Serializable]
public class DialogueLine
{
    public string speaker;
    public string text;
    public float duration;
    public DialogueType type = DialogueType.Normal;
}
public static class CSVParser
{
    public static List<DialogueLine> Parse(string csv)
    {
        List<DialogueLine> list = new List<DialogueLine>();
        string[] rows = csv.Split('\n');

        bool inArgument = false; // ★ 심문 영역 여부

        foreach (var raw in rows)
        {
            string row = raw.Trim();
            if (string.IsNullOrEmpty(row))
                continue;

            if (row.StartsWith("speaker") || row.StartsWith("이름") || row.StartsWith("Name"))
                continue;

            string[] cols = row.Split(',');
            if (cols.Length < 2)
                continue;

            DialogueLine line = new DialogueLine();
            line.speaker = cols[0].Trim();
            line.text = cols[1].Trim();

            // ● 타입 결정
            if (line.speaker == "심문 시작")
            {
                line.type = DialogueType.ArgumentStart;
                inArgument = true;
            }
            else if (line.speaker == "심문 종료")
            {
                line.type = DialogueType.ArgumentEnd;
                inArgument = false;
            }
            else if (inArgument)
            {
                line.type = DialogueType.Argument;  // ★ 심문 중의 모든 줄
            }
            else
            {
                line.type = DialogueType.Normal;
            }

            // ● duration
            if (cols.Length >= 3)
            {
                float dur;
                if (float.TryParse(cols[2].Trim(), out dur))
                    line.duration = dur;
            }

            list.Add(line);
        }

        return list;
    }
}

public class GoogleSheetLoader : MonoBehaviour
{
    [Header("일상 URL")]
    public string dialogueURL;     // 일상 대사

    [Header("재판 URL")]
    public string argumentURL;     // 재판 어그리먼트

    public IEnumerator LoadDialogueCSV(Action<List<DialogueLine>> onLoaded)
    {
        yield return LoadCSV(dialogueURL, onLoaded);
    }

    public IEnumerator LoadArgumentCSV(Action<List<DialogueLine>> onLoaded)
    {
        yield return LoadCSV(argumentURL, onLoaded);
    }

    private IEnumerator LoadCSV(string url, Action<List<DialogueLine>> onLoaded)
    {
        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("CSV 다운로드 실패: " + www.error);
            onLoaded?.Invoke(null);
            yield break;
        }

        string csvData = www.downloadHandler.text;

        List<DialogueLine> lines = CSVParser.Parse(csvData);

        onLoaded?.Invoke(lines);
    }
}
