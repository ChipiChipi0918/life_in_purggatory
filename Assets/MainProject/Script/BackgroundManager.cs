using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundManager : MonoBehaviour
{
    public static BackgroundManager instance;

    public GameObject tribunalBackground;
    public GameObject nomalBackground;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    public void DailyMapUpdate()
    {

    }

    public void ChangeNomalToTribunal()
    {
        tribunalBackground.SetActive(true);
        nomalBackground.SetActive(false);
    }
}
