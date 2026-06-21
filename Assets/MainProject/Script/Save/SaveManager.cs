using static ArgumentManager;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

[System.Serializable]
public class SaveData
{
    public int lineIndex;

    public int hp;

    public List<string> evidenceList = new();

    public HeroState heroState;

    public int currentBlockIndex;
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    public string GetPath(int slot)
    {
        return Application.persistentDataPath + $"/Save{slot}.json";
    }

    public void Save(int slot)
    {
        SaveData data = new SaveData();

        data.lineIndex = ArgumentManager.instance.CurrentLineIndex;
        data.hp = HpManager.instance.nowHp;
        data.heroState = ArgumentManager.instance.heroState;
        data.currentBlockIndex = ArgumentManager.instance.CurrentBlockIndex;

        foreach (var e in EvidenceManager.Instance.evidence)
        {
            data.evidenceList.Add(e.evidenceName);
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetPath(slot), json);

        Debug.Log("저장 완료");
    }

    public void Load(int slot)
    {
        string path = GetPath(slot);

        if (!File.Exists(path))
        {
            Debug.Log("저장 파일이 없습니다.");
            return;
        }

        SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));

        HpManager.instance.SetHp(data.hp);
        ArgumentManager.instance.heroState = data.heroState;

        EvidenceManager.Instance.ClearEvidence();

        foreach (var evidence in data.evidenceList)
        {
            EvidenceManager.Instance.AddEvidence(evidence);
        }

        ArgumentManager.instance.LoadGame(data);
    }
    public SaveData GetSaveData(int slot)
    {
        string path = GetPath(slot);

        if (!File.Exists(path))
            return null;

        return JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
    }
}