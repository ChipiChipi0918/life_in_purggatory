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
    public string speaker; //1
    public string text; //2
    //3
    public int effect;//4
    //5
    //6
    public float textTime; //7
    public string camFormat; //8
    public string addEvidence; //9
    public string showEvidence; //10
    public DialogueType type;

    public bool isChoice;
    public List<string> choices;
    public int correctIndex;
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

            string speaker = cols[0].Trim().Replace("\r", "");//1
            string text = cols[1].Trim().Replace("\\n", "\n");//2
            //3
            //4-int
            //5
            //6
            //7-float
            string camFormat = cols.Length > 7 ? cols[7].Trim().Replace("\r", "") : "";//8
            string addEvidence = cols.Length > 8 ? cols[8].Trim().Replace("\r", "") : "";//9
            string showEvidence = cols.Length > 9 ? cols[9].Trim().Replace("\r", "") : "";//10

            // ============================
            // 🔥 선택지 처리
            // ============================
            if (speaker == "선택지")
            {
                Debug.Log("선택지 CSV 발견!");

                DialogueLine choiceLine = new DialogueLine
                {
                    speaker = "",              // 선택지는 발화자 없음
                    text = "",                 // 텍스트 없음
                    type = DialogueType.Dialogue,
                    isChoice = true,
                    choices = new List<string>()
                };

                // 🔥 2열 텍스트: 선택1/선택2/선택3
                string[] rawChoices = text.Split('/');

                int correctIndex = -1;

                for (int c = 0; c < rawChoices.Length; c++)
                {
                    string ch = rawChoices[c].Trim();

                    // 🔥 *정답* 처리
                    if (ch.StartsWith("*") && ch.EndsWith("*"))
                    {
                        ch = ch.Substring(1, ch.Length - 2);  // 별 제거
                        correctIndex = c;
                    }

                    choiceLine.choices.Add(ch);
                }

                choiceLine.correctIndex = correctIndex;

                resultLines.Add(choiceLine);
                continue;
            }



            // ============================
            // 🔥 논의 시작
            // ============================
            if (speaker == "논의 시작" || speaker == "심문 시작")
            {
                inArgument = true;
                currentBlock = new ArgumentBlock();
                continue;
            }

            // ============================
            // 🔥 논의 종료
            // ============================
            if (speaker == "논의 종료" || speaker == "심문 종료")
            {
                inArgument = false;

                currentBlock.exitLine = new DialogueLine
                {
                    speaker = "엘리나",
                    text = text,
                    type = DialogueType.Dialogue,
                    textTime = 0
                };

                ArgumentManager.argumentBlocks.Add(currentBlock);
                continue;
            }


            // ============================
            // 🔥 일반 or Argument 대사 처리
            // ============================
            DialogueLine line = new DialogueLine
            {
                speaker = speaker,
                text = text,
                camFormat = camFormat,
                type = inArgument ? DialogueType.Argument : DialogueType.Dialogue,
                addEvidence = addEvidence,
                showEvidence = showEvidence
            };

            if (cols.Length >= 3 && int.TryParse(cols[3], out int type)) //6
                line.effect = type;
            if (cols.Length >= 6 && float.TryParse(cols[6], out float dur)) //6
                line.textTime = dur;

            if (inArgument)
                currentBlock.lines.Add(line);

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


