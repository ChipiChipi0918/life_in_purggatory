using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DialogueDirector : MonoBehaviour
{
    public static DialogueDirector instance;

    [Header("References")]
    [SerializeField] private Transform argumentCamTransform;
    [SerializeField] private GameObject nameTagObj;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject dialoguePanel;

    // 캐릭터 설정 데이터 (Director가 관리하는 것이 자연스러움)
    private readonly Dictionary<string, (float camPos, string colorCode)> characterConfig = new Dictionary<string, (float, string)>()
    {
        { "유은하", (0f, "#FFE2A0") },
        { "백현",   (-80f, "#0E432D") },
        { "유설희", (-60f, "#8F8F8F") },
        { "다니엘", (-40f, "#E7A300") },
        { "정희영", (-20f, "#9B2BFF") },
        { "장현우", (20f, "#CB1B00") },
        { "천주연", (40f, "#FFE945") },
        { "정태준", (60f, "#1572FF") },
        { "서진랑", (80f, "#5E3200") },
        { "Default", (0f, "#D9D9D9") }
    };

    private void Awake() => instance = this;

    #region Camera & Character Position
    public void MoveCam(string name, float xOffset, float duration = 0.5f)
    {
        float baseX = GetCharacterPos(name);
        argumentCamTransform.DOMoveX(baseX + xOffset, duration);
    }

    public void TpCam(string name)
    {
        float baseX = GetCharacterPos(name);
        argumentCamTransform.position = new Vector3(baseX, 0, -10);
    }

    public float GetCharacterPos(string name)
    {
        if (characterConfig.ContainsKey(name)) return characterConfig[name].camPos;
        return 0f;
    }

    public void MoveCharacter(string name, Vector3 pos)
    {
        // 캐릭터 이동 로직 구현
    }
    #endregion

    #region UI & Effects
    public void UpdateNameTag(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            nameTagObj.SetActive(false);
            return;
        }

        nameTagObj.SetActive(true);
        string colorCode = characterConfig.ContainsKey(name) ? characterConfig[name].colorCode : characterConfig["Default"].colorCode;

        string firstChar = name.Substring(0, 1);
        string restName = name.Substring(1);

        nameText.text = $"<size=180%><color={colorCode}>{firstChar}</color></size>{restName}";
    }

    public void ProcessScreenEffects(int effectId)
    {
        if (effectId == 1) EffectManager.instance.CameraShake();
        else if (effectId == 2) EffectManager.instance.Blood();
        else if (effectId == 3) EffectManager.instance.ShakeAndBlood();
        else if (effectId >= 10 && effectId <= 20) EffectManager.instance.Objection(effectId - 10);
        else if (effectId == 100) EffectManager.instance.FadeIn();
        else if (effectId == 101) EffectManager.instance.FadeOut();
    }

    public void ProcessEvidenceEffects(string addEv, string showEv)
    {
        if (EvidenceManager.Instance == null) return;
        if (!string.IsNullOrEmpty(addEv)) EvidenceManager.Instance.AddEvidence(addEv);
        if (!string.IsNullOrEmpty(showEv)) EvidenceManager.Instance.ShowEvidence(showEv);
    }
    #endregion
}