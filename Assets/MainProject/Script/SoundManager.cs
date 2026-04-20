using FMODUnity;
using FMOD.Studio; // 인스턴스 관리를 위해 필요합니다.
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    [Header("Dialogue Sound")]
    public EventReference eunhaVoice;

    [Header("UI")]
    public EventReference uiSelect;

    [Header("BGM")]
    public EventReference daily_01;
    public EventReference daily_04;

    public EventReference judgment_02;

    // 현재 재생 중인 BGM 인스턴스를 저장할 변수
    private EventInstance bgmInstance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public void EllinaVoice()
    {
        RuntimeManager.PlayOneShot(eunhaVoice);
    }

    public void UiSelect()
    {
        RuntimeManager.PlayOneShot(uiSelect);
    }

    public void BGM(string bgmName)
    {
        if(bgmName== "None") SoundManager.instance.StopBGM();
        else if(bgmName == "bgm_daily01") SoundManager.instance.BgmDaily_01();
    }

    public void BgmDaily_01()
    {
        PlayBGM(daily_01);
    }

    public void BgmDaily_04()
    {
        PlayBGM(daily_04);
    }

    public void BgmJudgment_02()
    {
        PlayBGM(judgment_02);
    }

    public void PlayBGM(EventReference bgmEvent)
    {
        if (bgmInstance.isValid())
        {
            bgmInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            bgmInstance.release();
        }

        // 2. 새로운 인스턴스 생성 및 시작
        bgmInstance = RuntimeManager.CreateInstance(bgmEvent);
        bgmInstance.start();
    }
    public void StopBGM()
    {
        if (bgmInstance.isValid())
        {
            bgmInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }
    }
}