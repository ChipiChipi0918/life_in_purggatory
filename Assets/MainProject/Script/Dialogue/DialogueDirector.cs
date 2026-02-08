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
    public List<Character> character = new List<Character>();

    [System.Serializable]
    public class Character
    {
        public GameObject body;
        public GameObject eyes;
        public GameObject mouth;
    }

    // 튜플 구조: (카메라X위치, 이름색상코드, 실제게임오브젝트)
    public Dictionary<string, (float camPos, string colorCode, GameObject obj)> characterConfig =
        new Dictionary<string, (float, string, GameObject)>()
    {
        { "리네", (100f, "#EAEAEA",null) },

        { "다니엘", (-40f, "#E7A300",null) },
        { "시몬",   (-80f, "#0E432D",null) },

        { "셀린", (40f, "#FFE945",null) },
        { "카를로스", (80f, "#5E3200",null) },
        { "로넌", (60f, "#1572FF",null) },

        { "리디아", (-100f, "#AFFFC7",null) },
        { "넬리", (-20f, "#9B2BFF",null) },
        { "에릭", (20f, "#CB1B00",null) },

        { "엘리나", (0f, "#FFE2A0",null) },
        { "실비아", (-60f, "#8F8F8F",null) },
        { "Default", (0f, "#D9D9D9",null) } // 기본값
    };

    private void Awake()
    {
        instance = this;
        InitCharacterDictionary();
    }

    // 리스트의 오브젝트들을 이름에 맞춰 딕셔너리에 매칭
    private void InitCharacterDictionary()
    {
        foreach (Character ch in character)
        {
            if (ch == null) continue;

            GameObject[] parts = { ch.body, ch.eyes, ch.mouth };

            foreach (var go in parts)
            {
                if (go == null) continue;

                if (characterConfig.TryGetValue(go.name, out var config))
                {
                    config.obj = go;
                    characterConfig[go.name] = config;
                }
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

            if(state == "Nomal")
            {
                //기본이라 뭐 없음
            }
            //추후 표정, 행동 추가 예정
        }
    }

    public void CharacterOn(List<string> names)
    {
        if (names == null || names.Count == 0) return;

        foreach (string name in names)
        {
            if (characterConfig.ContainsKey(name) && characterConfig[name].obj != null)
            {
                Debug.Log(characterConfig[name].obj + " 켜짐");
                ChatactorOnOff(characterConfig[name].obj, true);
            }
        }
    }

    public void CharacterOff(List<string> names)
    {
        if (names == null || names.Count == 0) return;

        foreach (string name in names)
        {
            if (characterConfig.ContainsKey(name) && characterConfig[name].obj != null)
            {
                Debug.Log(characterConfig[name].obj + " 꺼짐");
                ChatactorOnOff(characterConfig[name].obj, false);
            }
        }
    }

    private void ChatactorOnOff(GameObject target, bool a)
    {
        if (target == null) return;

        var renderers = target.GetComponentsInChildren<SpriteRenderer>();

        float alpha = a ? 1f : 0f;

        foreach (var sr in renderers)
        {
            sr.DOKill();
            sr.DOFade(alpha, 0.35f);
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