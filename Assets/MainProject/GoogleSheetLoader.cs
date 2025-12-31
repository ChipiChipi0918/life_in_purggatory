using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.EventSystems;
using KoreanTyper;

#region Dialogue Data Class
public enum DialogueType
{
    Dialogue,
    Argument
}

[System.Serializable]
public class DialogueLine
{
    public string speaker;
    public string text;
    public float duration;
    public DialogueType type;
}


#endregion

#region CSV Parser
public static class CSVParser
{
    public static List<DialogueLine> Parse(string csv)
    {
        List<DialogueLine> lines = new List<DialogueLine>();
        string[] rows = csv.Split('\n');

        bool inArgument = false;
        bool waitNextArgumentStart = false;  // 🔥 논의 종료 이후 대기 상태

        for (int i = 0; i < rows.Length; i++)
        {
            string row = rows[i].Trim();
            if (string.IsNullOrWhiteSpace(row)) continue;

            string[] cols = row.Split(',');
            if (cols.Length < 2) continue;

            string speaker = cols[0].Trim().Replace("\r", "");
            string text = cols[1].Trim().Replace("\\n", "\n");

            // 🔥 논의 시작
            if (speaker == "논의 시작")
            {
                inArgument = true;
                waitNextArgumentStart = false; // 다시 시작
                continue;
            }

            // 🔥 논의 종료
            if (speaker == "논의 종료")
            {
                inArgument = false;
                waitNextArgumentStart = true;  // 다음 논의 시작까지 스킵 모드
                speaker = "";
                continue;
            }

            // 🔥 논의 종료 이후, 다음 논의 시작 전이면 스킵
            if (waitNextArgumentStart)
            {
                continue;
            }

            DialogueLine line = new DialogueLine
            {
                speaker = speaker,
                text = text,
                type = inArgument ? DialogueType.Argument : DialogueType.Dialogue
            };

            if (cols.Length >= 3 && float.TryParse(cols[2], out float dur))
                line.duration = dur;

            lines.Add(line);
        }

        return lines;
    }
}


#endregion


#region Google Sheet Loader

public class GoogleSheetLoader : MonoBehaviour
{
    [Header("일상 URL")]
    public string dialogueURL;

    [Header("재판 URL")]
    public string argumentURL;

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
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string csv = request.downloadHandler.text;
                var list = CSVParser.Parse(csv);
                onLoaded?.Invoke(list);
            }
            else
            {
                Debug.LogError("CSV Load Failed: " + request.error);
                onLoaded?.Invoke(new List<DialogueLine>());
            }
        }
    }
}

#endregion


