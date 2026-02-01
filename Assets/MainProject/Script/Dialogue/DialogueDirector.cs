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

    [Header("Character List (Assign in Inspector)")]
    public List<GameObject> character = new List<GameObject>();

    // 튜플 구조: (카메라X위치, 이름색상코드, 실제게임오브젝트)
    public Dictionary<string, (float camPos, string colorCode, GameObject obj)> characterConfig =
        new Dictionary<string, (float, string, GameObject)>()
    {
        { "유은하", (0f, "#FFE2A0", null) },
        { "백현",   (-80f, "#0E432D", null) },
        { "유설희", (-60f, "#8F8F8F", null) },
        { "다니엘", (-40f, "#E7A300", null) },
        { "정희영", (-20f, "#9B2BFF", null) },
        { "장현우", (20f, "#CB1B00", null) },
        { "천주연", (40f, "#FFE945", null) },
        { "정태준", (60f, "#1572FF", null) },
        { "서진랑", (80f, "#5E3200", null) },
        { "Default", (0f, "#D9D9D9", null) }
    };

    private void Awake()
    {
        instance = this;
        InitCharacterDictionary();
    }

    // 리스트의 오브젝트들을 이름에 맞춰 딕셔너리에 매칭
    private void InitCharacterDictionary()
    {
        foreach (GameObject go in character)
        {
            if (go != null && characterConfig.ContainsKey(go.name))
            {
                var config = characterConfig[go.name];
                config.obj = go;
                characterConfig[go.name] = config;
            }
        }
    }

    #region Camera & Character Control
    public void MoveCam(string name, float xOffset, float duration = 0.5f)
    {
        float baseX = GetCharacterPos(name);
        argumentCamTransform.DOMoveX(baseX + xOffset, duration).SetEase(Ease.OutCubic);
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

    public void MoveCharacter(string name, float duration, Vector3 targetPos)
    {
        if (characterConfig.ContainsKey(name) && characterConfig[name].obj != null)
        {
            Debug.Log($"{name} 이동: {targetPos}");
            GameObject target = characterConfig[name].obj;

            // 기존 애니메이션 중복 방지를 위해 Kill 후 실행
            target.transform.DOKill();
            target.transform.DOLocalMove(targetPos, duration).SetEase(Ease.OutQuad);
        }
    }

    public void CharacterState(string name, string state)
    {
        if (characterConfig.ContainsKey(name) && characterConfig[name].obj != null)
        {
            Debug.Log($"{name} 상태 변경: {state}");
            GameObject target = characterConfig[name].obj;

            if(state == "Off")
            {
                ChatactorOnOff(target,false);
            }
            else if(state == "Nomal")
            {
                ChatactorOnOff(target,true);
                //기본이라 뭐 없음
            }
            //추후 표정, 행동 추가 예정
        }
    }

    private void ChatactorOnOff(GameObject target ,bool a)
    {
        if (a)
        {
            target.gameObject.GetComponent<SpriteRenderer>().DOFade(1f, 0.35f);
        }
        else
        {
            target.gameObject.GetComponent<SpriteRenderer>().DOFade(0f, 0.35f);
        }
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
        string restName = name.Length > 1 ? name.Substring(1) : "";

        nameText.text = $"<size=180%><color={colorCode}>{firstChar}</color></size>{restName}";
    }

    public void ProcessScreenEffects(int effectId)
    {
        if (EffectManager.instance == null) return;

        switch (effectId)
        {
            case 1: EffectManager.instance.CameraShake(); break;
            case 2: EffectManager.instance.Blood(); break;
            case 3: EffectManager.instance.ShakeAndBlood(); break;
            case 100: EffectManager.instance.FadeIn(); break;
            case 101: EffectManager.instance.FadeOut(); break;
            default:
                if (effectId >= 10 && effectId <= 20) EffectManager.instance.Objection(effectId - 10);
                break;
        }
    }

    public void ProcessEvidenceEffects(string addEv, string showEv)
    {
        if (EvidenceManager.Instance == null) return;
        if (!string.IsNullOrEmpty(addEv)) EvidenceManager.Instance.AddEvidence(addEv);
        if (!string.IsNullOrEmpty(showEv)) EvidenceManager.Instance.ShowEvidence(showEv);
    }
    #endregion
}