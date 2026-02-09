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

    [System.Serializable]
    public class CharacterObjClass
    {
        public string name; // 캐릭터 이름 (딕셔너리 매칭용)
        public SpriteRenderer bodyRenderer; // Root 역할을 겸함
        public SpriteRenderer eyesRenderer;
        public SpriteRenderer mouthRenderer;
    }

    [System.Serializable]
    public class CharacterStateClass
    {
        public string characterName;
        public List<Sprite> body;
        public List<Sprite> eyes;
        public List<Sprite> mouth;
    }

    [Header("일상 파트 캐릭터 리스트")]
    public List<CharacterObjClass> character = new List<CharacterObjClass>();

    [Header("캐릭터 몸짓, 표정 스프라이트 리스트")]
    public List<CharacterStateClass> characterState = new List<CharacterStateClass>();

    // 통합 데이터 맵: (카메라X위치, 이름색상코드, 오브젝트참조, 상태데이터참조)
    public Dictionary<string, (float camPos, string colorCode, CharacterObjClass obj, CharacterStateClass state)> characterConfig
        = new Dictionary<string, (float, string, CharacterObjClass, CharacterStateClass)>();

    private void Awake()
    {
        instance = this;
        InitCharacterDictionary();
    }

    private void InitCharacterDictionary()
    {
        // 1. 기본 설정값 세팅
        var baseData = new Dictionary<string, (float pos, string color)>()
        {
            { "리네", (100f, "#EAEAEA") }, { "다니엘", (-40f, "#E7A300") },
            { "시몬", (-80f, "#0E432D") }, { "셀린", (40f, "#FFE945") },
            { "카를로스", (80f, "#5E3200") }, { "로넌", (60f, "#1572FF") },
            { "리디아", (-100f, "#AFFFC7") }, { "넬리", (-20f, "#9B2BFF") },
            { "에릭", (20f, "#CB1B00") }, { "엘리나", (0f, "#FFE2A0") },
            { "실비아", (-60f, "#8F8F8F") }, { "Default", (0f, "#D9D9D9") }
        };

        // 2. 리스트에 있는 데이터를 이름 기준으로 매칭하여 캐싱
        foreach (var data in baseData)
        {
            var objRef = character.Find(x => x.name == data.Key);
            var stateRef = characterState.Find(x => x.characterName == data.Key);
            characterConfig[data.Key] = (data.Value.pos, data.Value.color, objRef, stateRef);
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
        if (characterConfig.TryGetValue(name, out var data) && data.obj != null)
        {
            // bodyRenderer가 Root이므로 이를 이동시키면 자식들도 함께 이동
            Transform t = data.obj.bodyRenderer.transform;
            t.DOKill();
            t.DOLocalMove(targetPos, duration).SetEase(Ease.OutQuad);
        }
    }

    /// <summary>
    /// 캐릭터 표정 변경 (state: [몸, 눈, 입] 인덱스 리스트)
    /// </summary>
    public void CharacterState(string name, List<string> state)
    {
        if (!characterConfig.TryGetValue(name, out var data) || data.obj == null || data.state == null) return;

        // 각 파트별 스프라이트 교체 로직
        SetPartSprite(data.obj.bodyRenderer, data.state.body, state, 0);
        SetPartSprite(data.obj.eyesRenderer, data.state.eyes, state, 1);
        SetPartSprite(data.obj.mouthRenderer, data.state.mouth, state, 2);
    }

    private void SetPartSprite(SpriteRenderer sr, List<Sprite> spriteList, List<string> indices, int listIdx)
    {
        if (sr == null || indices.Count <= listIdx) return;
        if (int.TryParse(indices[listIdx], out int spriteIdx))
        {
            if (spriteIdx >= 0 && spriteIdx < spriteList.Count)
                sr.sprite = spriteList[spriteIdx];
        }
    }

    public void CharacterOn(List<string> names)
    {
        if (names == null) return;
        foreach (string name in names)
            if (characterConfig.TryGetValue(name, out var data)) ChatactorOnOff(data.obj, true);
    }

    public void CharacterOff(List<string> names)
    {
        if (names == null) return;
        foreach (string name in names)
            if (characterConfig.TryGetValue(name, out var data)) ChatactorOnOff(data.obj, false);
    }

    private void ChatactorOnOff(CharacterObjClass target, bool isOn)
    {
        if (target == null) return;
        float alpha = isOn ? 1f : 0f;

        // 본체, 눈, 입 모두 페이드 처리
        var renderers = new[] { target.bodyRenderer, target.eyesRenderer, target.mouthRenderer };
        foreach (var sr in renderers)
        {
            if (sr == null) continue;
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