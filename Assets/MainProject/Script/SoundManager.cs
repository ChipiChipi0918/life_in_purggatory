using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    [Header("Dialouge Sound")]
    public EventReference eunhaVoice;

    [Header("Ui")]
    public EventReference uiSelect;

    private void Awake()
    {
        if(instance == null) instance = this;
    }

    public void EunhaVoice()
    {
        RuntimeManager.CreateInstance(eunhaVoice).start();
    }

    public void UiSelect()
    {
        RuntimeManager.CreateInstance(uiSelect).start();
    }
}
