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
public class ArgumentBlock
{
    public List<DialogueLine> lines = new List<DialogueLine>();
    public DialogueLine exitLine;   // 논의 종료 후 출력될 일반 대사
}

public static class CSVParser
{
    public static List<DialogueLine> Parse(string csv)
    {
        List<DialogueLine> resultLines = new List<DialogueLine>();

        string[] rows = csv.Split('\n');

        bool inArgument = false;
        ArgumentBlock currentBlock = null;

        if (ArgumentManager.argumentBlocks == null)
            ArgumentManager.argumentBlocks = new List<ArgumentBlock>();

        for (int i = 0; i < rows.Length; i++)
        {
            string row = rows[i].Trim();
            if (string.IsNullOrWhiteSpace(row)) continue;

            string[] cols = row.Split(',');
            if (cols.Length < 2) continue;

            string speaker = cols[0].Trim().Replace("\r", "");
            string text = cols[1].Trim().Replace("\\n", "\n");

            // 논의 시작
            if (speaker == "논의 시작")
            {
                inArgument = true;
                currentBlock = new ArgumentBlock();
                continue;
            }

            // 논의 종료
            if (speaker == "논의 종료")
            {
                inArgument = false;

                // 종료 후 출력될 대사 저장
                currentBlock.exitLine = new DialogueLine
                {
                    speaker = "엘리나",
                    text = text,
                    type = DialogueType.Dialogue,
                    duration = 0
                };

                ArgumentManager.argumentBlocks.Add(currentBlock);
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

            // argument block 내부면 블록에 추가
            if (inArgument)
                currentBlock.lines.Add(line);

            // 전체 라인에도 추가 (일반 대사)
            resultLines.Add(line);
        }

        return resultLines;
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


