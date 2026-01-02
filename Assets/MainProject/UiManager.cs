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

    [Header("Shake")]
    public GameObject hitUi;
    public bool usingShake = false;
    public float m_roughness;      // 거칠기 정도
    public float m_magnitude;      // 움직임 범위
    public float m_rotation = 1f;       // 회전 강도 (카메라 쉐이크 강도)

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
            camRotate(new Vector3(0, 0, 2.5f));
        }
        else
        {
            StartCoroutine(ArgumentUiOnCoroutine(argumentEndUi));
            camRotate(new Vector3(0, 0, 0f));
        }
    }

    public void camRotate(Vector3 a)
    {
        camTransform.DORotate(a, 0.01f);
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

    public void Shaking(float parametersShakeTime) // 0.35는 작은 쉐이크 and 0.7은 큰 쉐이크
    {
        usingShake = true;
        StartCoroutine(Shake(parametersShakeTime)); // 코루틴 실행
    }
    IEnumerator Shake(float duration)
    {
        hitUi.SetActive(true);
        
        
        Vector3 defotCamPos = camTransform.position;

        duration *= 1.5f;

        float halfDuration = duration / 2;
        float elapsed = 0f;
        float tick = Random.Range(-1f, 1f); //5와 -5 사이의 랜덤 값 을 변수에 저장

        while (elapsed < duration) //elapsed가 duration보다 작을 동안 반복
        {
            elapsed += Time.deltaTime / halfDuration; //elapsed에 1프레임 동안 걸리는 시간을 더함 (+halfDuration으로 나누기)

            tick += Time.deltaTime * m_roughness; // tick에 1프레임 동안 걸리는 시간 곱하기 m_roughness 를 더함
            Vector3 position = camTransform.position; // 현재 위치를 포지션에 저장
            camTransform.rotation = Quaternion.Euler(new Vector3(0, 0, tick * duration * m_rotation));
            position.y += (Mathf.PerlinNoise(tick * 0.5f, 0) - 0.5f) * m_magnitude * Mathf.PingPong(elapsed, halfDuration); // 부드럽게 이동
            camTransform.position = position; // 계산된 포지션으로 이동


            yield return null; // 중단
        }
        hitUi.SetActive(false);

        camTransform.position = defotCamPos;
        camTransform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
        usingShake = false;
    }
}
