using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    [Header("Dialouge Sound")]
    public EventReference elinaVoice;

    private void Awake()
    {
        if(instance == null) instance = this;
    }

    public void ElinaVoice()
    {
        RuntimeManager.CreateInstance(elinaVoice).start();
    }
}
