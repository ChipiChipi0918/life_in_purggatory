using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GoogleSheetLoader : MonoBehaviour
{
    [Header("CSV 링크 (웹으로 게시한 URL)")]
    public string csvUrl;

    public IEnumerator LoadDialogue(System.Action<List<DialogueLine>> callback)
    {
        UnityWebRequest req = UnityWebRequest.Get(csvUrl);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Google Sheet 다운로드 실패: " + req.error);
            callback?.Invoke(null);
            yield return null;
        }

        string csvText = req.downloadHandler.text;
        List<DialogueLine> lines = ParseCSV(csvText);

        callback?.Invoke(lines);
    }

    private List<DialogueLine> ParseCSV(string csv)
    {
        List<DialogueLine> list = new List<DialogueLine>();

        // 줄단위로 나누기
        string[] rows = csv.Split('\n');

        foreach (var row in rows)
        {
            // 빈 줄 건너뛰기
            if (string.IsNullOrWhiteSpace(row))
                continue;

            // 한 행은 "캐릭터,대사"
            string[] cols = row.Split(',');

            if (cols.Length < 2)
                continue;

            DialogueLine line = new DialogueLine
            {
                speaker = cols[0].Trim(),
                text = cols[1].Trim()
            };

            list.Add(line);
        }

        return list;
    }

}

[System.Serializable]
public class DialogueLine
{
    public string speaker;
    public string text;
}
