using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UiManager : MonoBehaviour
{
    public static UiManager instance;

    public bool isUiAnim;

    public Transform camTransform;

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
        {
            StartCoroutine(ArgumentUiOnCoroutine(argumentStartUi));
            camTransform.DORotate(new Vector3(0, 0, 2.5f), 0.01f);
        }
        else
        {
            StartCoroutine(ArgumentUiOnCoroutine(argumentEndUi));
            camTransform.DORotate(new Vector3(0, 0, 0f), 0.01f);
        }
    }
    
    IEnumerator ArgumentUiOnCoroutine(Transform startOrEnd)
    {
        Time.timeScale = 0.01f;
        isUiAnim = true;
        startOrEnd.DOScaleY(1, 0.005f);
        yield return new WaitForSecondsRealtime(3);
        Time.timeScale = 1f;
        startOrEnd.DOScaleY(0, 1);
        isUiAnim = false;
        yield return new WaitForSecondsRealtime(1);
        startOrEnd.localScale = new Vector3(1, 0, 1);
    }
}
