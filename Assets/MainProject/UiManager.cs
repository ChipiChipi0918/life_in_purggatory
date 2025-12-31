using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UiManager : MonoBehaviour
{
    public static UiManager instance;

    public bool isUiAnim;

    [Header("논의 시작/종료")]
    public Transform argumentStartUi;
    public Transform argumentEndUi;

    private void Awake()
    {
        if(instance == null) instance = this;

        argumentStartUi.localScale = new Vector3(1, 0, 1);
        argumentEndUi.localScale = new Vector3(1, 0, 1);
    }

    public void ArgumentUiOn(bool isStart)
    {
        if (isStart)
            StartCoroutine(ArgumentUiOnCoroutine(argumentStartUi));
        else
            StartCoroutine(ArgumentUiOnCoroutine(argumentEndUi));
    }
    
    IEnumerator ArgumentUiOnCoroutine(Transform startOrEnd)
    {
        Time.timeScale = 0.01f;
        startOrEnd.DOScaleY(1, 0.5f * Time.unscaledDeltaTime);
        yield return new WaitForSecondsRealtime(3);
        startOrEnd.DOScaleY(0, 1);
        Time.timeScale = 1f;
    }
}
