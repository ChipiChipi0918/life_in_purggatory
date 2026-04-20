using FMOD.Studio; // 인스턴스 관리를 위해 필요합니다.
using FMODUnity;
using System.Xml.Linq;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    [Header("Dialogue Sound")]
    public EventReference eunhaVoice;

    [Header("UI")]
    public EventReference uiSelect;


    [Header("SFX")]
    public EventReference stabbed_02;
    public EventReference door_open_05;
    public EventReference item_get_06;
    public EventReference run_07;
    public EventReference run2_07;
    public EventReference walking_08;
    public EventReference walking2_08;
    public EventReference happy_09;
    public EventReference gun_shot_10;

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

    public void SFX(string sfxName)
    {
        Debug.Log(sfxName+" 사운드 재생");

        if (sfxName == "stabbed_02") RuntimeManager.PlayOneShot(stabbed_02);
        else if (sfxName == "door_open_05") RuntimeManager.PlayOneShot(door_open_05);
        else if (sfxName == "item_get_06") RuntimeManager.PlayOneShot(item_get_06);
        else if (sfxName == "run_07") RuntimeManager.PlayOneShot(run_07);
        else if (sfxName == "run2_07") RuntimeManager.PlayOneShot(run2_07);
        else if (sfxName == "walking_08") RuntimeManager.PlayOneShot(walking_08);
        else if (sfxName == "walking2_08") RuntimeManager.PlayOneShot(walking2_08);
        else if (sfxName == "happy_09") RuntimeManager.PlayOneShot(happy_09);
        else if (sfxName == "gun_shot_10") RuntimeManager.PlayOneShot(gun_shot_10);
    }

    #region bgm

    public void BGM(string bgmName)
    {
        Debug.Log(bgmName + " 배경음 재생");

        if (bgmName== "None") StopBGM();
        else if(bgmName == "bgm_daily_01") PlayBGM(daily_01);
        else if(bgmName == "bgm_daily_04") PlayBGM(daily_04);
        else if (bgmName == "bgm_judgment_02") PlayBGM(judgment_02);
    }
    void PlayBGM(EventReference bgmEvent)
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
    #endregion
}