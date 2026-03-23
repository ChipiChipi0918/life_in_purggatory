using KoreanTyper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using static ArgumentManager;

#region Dialogue Data Class
public enum DialogueType
{
    Dialogue,
    Argument,
    PlaceSelection // 🔥 [추가] 장소 지적 타입
}
public enum ArgumentActionType
{
    None,
    Refutation, // 반론
    Agreement   // 찬성
}

[System.Serializable]
public class DialogueLine
{
    public string speaker; //1
    public string text; //2

    public Vector3 characterPos; //3
    public string charState;//4
    public List<string> charStateList = new List<string>();

    public string charOn;//5
    public string charOff;//6
    public List<string> charOnList = new List<string>();
    public List<string> charOffList = new List<string>();

    public int effect;//7
    public string soundEffect;//8

    public Vector3 cameraPos;//9

    public float textTime; //10
    public string camFormat; //11

    public string addEvidence; //12
    public string showEvidence; //13

    public string background; //17
    public string bgm; //18
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
    public DialogueLine exitLine;

    public string correctEvidence;
    public List<string> evidenceCandidates;

    public ActState actionType;
}

public static class CSVUtil
{
    // "안녕, 반가워" 처럼 콤마 포함 텍스트 대응
    public static string[] SplitCSV(string line)
    {
        var pattern = @",(?=(?:[^""]*""[^""]*"")*[^""]*$)";
        var values = Regex.Split(line, pattern);

        for (int i = 0; i < values.Length; i++)
        {
            values[i] = values[i].Trim();

            // 앞뒤 따옴표 제거
            if (values[i].StartsWith("\"") && values[i].EndsWith("\""))
            {
                values[i] = values[i].Substring(1, values[i].Length - 2);
            }
        }
        return values;
    }
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

        for (int i = 1; i < rows.Length; i++)
        {
            string row = rows[i].Trim();
            if (string.IsNullOrWhiteSpace(row)) continue;

            string[] cols = CSVUtil.SplitCSV(row);
            if (cols.Length < 2) continue;

            string speaker = cols[0].Trim().Replace("\r", "");//1
            string text = cols[1].Trim().Replace("\\n", "\n");//2
            #region 3열 (캐릭터 위치)값 파싱
            Vector3 charPos = Vector3.zero;
            if (cols.Length > 2 && !string.IsNullOrEmpty(cols[2]))
            {
                string[] posValues = cols[2].Split('/');
                if (posValues.Length == 3)
                {
                    float.TryParse(posValues[0], out float x);
                    float.TryParse(posValues[1], out float y);
                    float.TryParse(posValues[2], out float z);
                    //charPos = new Vector3(x, y, z);
                    charPos = new Vector3(x, y, z)/100;
                }
            }
            #endregion //3
            string charState = cols[3].Trim().Replace("\\n", "\n");//4
            string charOn = cols[4].Trim().Replace("\\n", "\n");//5
            string charOff = cols[5].Trim().Replace("\\n", "\n");//6
            //7-int effect
            //8 효과음 들어갈 예정
            #region 9열 (카메라 위치)값 파싱
            Vector3 camPos = Vector3.zero;
            if (cols.Length > 2 && !string.IsNullOrEmpty(cols[8]))
            {
                string[] posValues = cols[8].Split('/');
                if (posValues.Length == 3)
                {
                    float.TryParse(posValues[0], out float x);
                    float.TryParse(posValues[1], out float y);
                    float.TryParse(posValues[2], out float z);
                    camPos = new Vector3(x, y, z) / 100;
                }
            }
            #endregion //9
            //10-float 논의 텍스트 시간(길이)
            string camFormat = cols.Length > 10 ? cols[10].Trim().Replace("\r", "") : "";//11
            string addEvidence = cols.Length > 11 ? cols[11].Trim().Replace("\r", "") : "";//12
            string showEvidence = cols.Length > 12 ? cols[12].Trim().Replace("\r", "") : "";//13

            string background = cols.Length > 16 ? cols[16].Trim().Replace("\n", "") : "";//17
            string bgm = cols.Length > 17 ? cols[17].Trim().Replace("\n", "") : "";//18
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
                    characterPos = charPos,
                    charState = charState,
                    charOn = charOn,
                    charOff = charOff,
                    cameraPos = camPos,
                    type = DialogueType.Dialogue,
                    isChoice = true,
                    choices = new List<string>(),
                    background = "",
                    bgm = "",
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
            // 🔥 장소 지적 처리
            // ============================
            if (speaker == "장소 지적")
            {
                Debug.Log($"장소 지적 CSV 발견! 정답: {text}");

                DialogueLine placeLine = new DialogueLine
                {
                    speaker = "",       // 시스템 메시지 혹은 공란
                    text = text.Trim(),       // 🔥 CSV 2열(text) 값을 정답으로 저장
                    type = DialogueType.PlaceSelection, // 타입 설정
                    textTime = 0              // 즉시 처리되도록 0 설정 (필요시 조절)
                };

                resultLines.Add(placeLine);
                continue; // 다음 루프로 이동
            }

            // ============================
            // 🔥 논의 시작
            // ============================
            if (speaker.StartsWith("논의 시작"))
            {
                inArgument = true;
                currentBlock = new ArgumentBlock();

                // 🔥 [추가] 화자 텍스트를 분석하여 행동 타입 분류
                if (speaker.Contains("반론"))
                {
                    currentBlock.actionType = ActState.counterargument;
                }
                else if (speaker.Contains("찬성"))
                {
                    currentBlock.actionType = ActState.agreement;
                }

                if (!string.IsNullOrEmpty(text))
                {
                    string[] raw = text.Split('/');
                    currentBlock.evidenceCandidates = new List<string>();

                    foreach (string r in raw)
                    {
                        string ev = r.Trim();

                        if (ev.StartsWith("*") && ev.EndsWith("*"))
                        {
                            ev = ev.Substring(1, ev.Length - 2);
                            currentBlock.correctEvidence = ev;
                        }

                        currentBlock.evidenceCandidates.Add(ev);
                    }

                    if (string.IsNullOrEmpty(currentBlock.correctEvidence))
                        Debug.LogWarning($"{speaker} : 정답 증거품이 지정되지 않았습니다.");
                }
                continue;
            }

            // ============================
            // 🔥 논의 종료
            // ============================
            if (speaker == "논의 종료" )
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
                characterPos = charPos,
                charState = charState,
                charOn = charOn,
                charOff = charOff,
                cameraPos = camPos,
                camFormat = camFormat,
                type = inArgument ? DialogueType.Argument : DialogueType.Dialogue,
                addEvidence = addEvidence,
                showEvidence = showEvidence,
                background = background,
                bgm = bgm
            };

            // 🔥 [추가] charOn 문자열 리스트화
            if (!string.IsNullOrEmpty(charState))
            {
                string[] names = charState.Split('/');
                foreach (string name in names)
                {
                    string trimmedName = name.Trim();
                    if (!string.IsNullOrEmpty(trimmedName))
                        line.charStateList.Add(trimmedName);
                }
            }

            // 🔥 [추가] charOn 문자열 리스트화
            if (!string.IsNullOrEmpty(charOn))
            {
                string[] names = charOn.Split('/');
                foreach (string name in names)
                {
                    string trimmedName = name.Trim();
                    if (!string.IsNullOrEmpty(trimmedName))
                        line.charOnList.Add(trimmedName);
                }
            }

            // 🔥 [추가] charOff 문자열 리스트화
            if (!string.IsNullOrEmpty(charOff))
            {
                string[] names = charOff.Split('/');
                foreach (string name in names)
                {
                    string trimmedName = name.Trim();
                    if (!string.IsNullOrEmpty(trimmedName))
                        line.charOffList.Add(trimmedName);
                }
            }

            if (cols.Length >= 6 && int.TryParse(cols[6], out int type)) //7
                line.effect = type;
            if (cols.Length >= 9 && float.TryParse(cols[9], out float dur)) //10
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
    public int chapter = 1;

    [Header("Chapter1 URL")]
    public string Chapter1_DailyURL;
    public string Chapter1_JudgmentURL;

    [Header("Chapter2 URL")]
    public string Chapter2_DailyURL;
    public string Chapter2_JudgmentURL;

    [Header("Chapter3 URL")]
    public string Chapter3_DailyURL;
    public string Chapter3_JudgmentURL;

    public IEnumerator LoadDialogueCSV(Action<List<DialogueLine>> onLoaded)
    {
        yield return LoadCSV(Chapter1_DailyURL, onLoaded);
    }

    public IEnumerator LoadArgumentCSV(Action<List<DialogueLine>> onLoaded)
    {
        yield return LoadCSV(Chapter1_JudgmentURL, onLoaded);
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


