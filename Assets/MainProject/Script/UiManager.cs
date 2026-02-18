using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UiManager : MonoBehaviour
{
    public static UiManager instance;

    public bool isUiAnim;

    public Transform camTransform;

    [Header("논의 시작 시 나오는 Ui")]
    public RectTransform argumentEvidenceUi;
    public RectTransform argumentActUi;
    public RectTransform argumentTimeUi;

    [Header("Back Ui")]
    public CanvasGroup backUi;

    [Header("로그")] //f1
    public GameObject logue;
    public bool isLogue;

    [Header("Hotel information")] //f2
    public RectTransform hotelInformation;
    public bool isHotelInformation;

    [Header("Ui Off")] //f3
    public GameObject allUi; //캔버스
    private bool isUiOff;

    [Header("맵 지적")]
    public RectTransform mapPointOut;
    public bool isMapPointOut;

    [Header("논의 시작/종료")]
    public Transform argumentStartUi;
    public Transform argumentEndUi;

    public RectTransform argumentLineUp;
    public RectTransform argumentLineDown;

    [Header("Shake")]
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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1) && isUiAnim == false)
        {
            isLogue = !isLogue;
            StartCoroutine(LogueCoroutine(isLogue));
        }
        if (Input.GetKeyDown(KeyCode.F2) && isUiAnim==false)
        {
            isHotelInformation = !isHotelInformation;
            StartCoroutine(HotelInformationCoroutine(isHotelInformation));
        }
        if (Input.GetKeyDown(KeyCode.F3) && isUiOff==false)
        {
            Debug.Log("DW");
            isUiOff = true;
            allUi.gameObject.SetActive(false);
        }
        if ((Input.GetKeyDown(KeyCode.F3) || Input.GetMouseButtonDown(0)) && isUiOff==true)
        {
            isUiOff = false;
            allUi.gameObject.SetActive(true);
        }
    }

    
    private IEnumerator BackUiFade(bool InOrOut) //true 켜기 /false 끄기
    {

        backUi.alpha = 0f;
        backUi.gameObject.SetActive(true);

        if (InOrOut)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime;
                backUi.alpha = Mathf.Lerp(0f, 1f, t / 0.5f);
                yield return null;
            }
            backUi.alpha = 1f;
        }
        else
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime;
                backUi.alpha = Mathf.Lerp(0f, 1f, 1 - (t / 0.5f));
                yield return null;
            }
            backUi.alpha = 0f;
        }
    }

    private IEnumerator HotelInformationCoroutine(bool a)
    {
        isUiAnim = true;
        if (a)
        {
            StartCoroutine(BackUiFade(true));

            Time.timeScale = 0;

            hotelInformation.gameObject.SetActive(isHotelInformation);
            hotelInformation.DOAnchorPosY(0,1).SetUpdate(true);
            yield return new WaitForSecondsRealtime(1f);
        }
        else
        {
            StartCoroutine(BackUiFade(false));

            Time.timeScale = 1;

            hotelInformation.DOAnchorPosY(-1490, 1).SetUpdate(true);
            yield return new WaitForSecondsRealtime(1f);
            hotelInformation.gameObject.SetActive(isHotelInformation);
        }
       
        isUiAnim = false;
    }

    private IEnumerator LogueCoroutine(bool a)
    {
        isUiAnim = true;
        if (a)
        {
            StartCoroutine(BackUiFade(true));
            logue.SetActive(true);
            Time.timeScale = 0;

            yield return new WaitForSecondsRealtime(1f);
        }
        else
        {
            StartCoroutine(BackUiFade(false));
            logue.SetActive(false);
            Time.timeScale = 1;

            yield return new WaitForSecondsRealtime(1f);
        }

        isUiAnim = false;

        yield return null;
    }

    public void MapPointOutUiOn(bool a)
    {
        StartCoroutine(MapPointOutUiCoroutine(a));
    }

    private IEnumerator MapPointOutUiCoroutine(bool a)
    {
        isUiAnim = true;
        if (a)
        {
            StartCoroutine(BackUiFade(true));

            Time.timeScale = 0;

            mapPointOut.DOAnchorPosY(0, 1).SetUpdate(true);
            yield return new WaitForSecondsRealtime(1f);
        }
        else
        {
            StartCoroutine(BackUiFade(false));

            Time.timeScale = 1;

            mapPointOut.DOAnchorPosY(-1490, 1).SetUpdate(true);
            yield return new WaitForSecondsRealtime(1f);
        }

        isUiAnim = false;
    }

    // ArgumentUiOn 메서드를 아래와 같이 수정하세요.
    public void ArgumentUiOn(bool isStart, float rotationZ = 2.5f)
    {
        OnArgumentEvidence(isStart);

        if (isStart)
        {
            StartCoroutine(ArgumentUiOnCoroutine(argumentStartUi));
            camRotate(new Vector3(0, 0, rotationZ), 1);
            camTransform.transform.DOMoveZ(-4.36f, 1);
        }
        else
        {
            StartCoroutine(ArgumentUiOnCoroutine(argumentEndUi));
            camRotate(new Vector3(0, 0, 0), 1);
            camTransform.transform.DOMoveZ(-10f, 1);
        }
    }
    public void OnArgumentEvidence(bool On)
    {
        if (On)
        {
            argumentEvidenceUi.DOAnchorPosX(376, 1).SetUpdate(false);
            argumentActUi.DOAnchorPosX(376, 1).SetUpdate(false);
            argumentTimeUi.DOAnchorPosX(-111,1).SetUpdate(false);

            argumentLineUp.DOAnchorPosY(0, 1).SetUpdate(true);
            argumentLineDown.DOAnchorPosY(0, 1).SetUpdate(true);
        }
        else
        {
            argumentEvidenceUi.DOAnchorPosX(548, 1).SetUpdate(true);
            argumentActUi.DOAnchorPosX(548, 1).SetUpdate(true);
            argumentTimeUi.DOAnchorPosX(268,1).SetUpdate(true);

            argumentLineUp.DOAnchorPosY(75, 1).SetUpdate(true);
            argumentLineDown.DOAnchorPosY(-75, 1).SetUpdate(true);
        }
    }


    public void camRotate(Vector3 a, float speed)
    {
        camTransform.DORotate(a, speed).SetUpdate(true);
    }
    
    IEnumerator ArgumentUiOnCoroutine(Transform startOrEnd)
    {
        StartCoroutine(BackUiFade(true));

        startOrEnd.localScale = new Vector3(1, 0, 1);
        Time.timeScale = 0.01f;
        isUiAnim = true;
        startOrEnd.DOScaleY(1, 0.5f).SetUpdate(true);
        yield return new WaitForSecondsRealtime(3);
        Time.timeScale = 1f;
        startOrEnd.DOScaleY(0, 1).SetUpdate(true);
        isUiAnim = false;
        StartCoroutine(BackUiFade(false));
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
        camTransform.position = defotCamPos;
        camTransform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
        usingShake = false;
    }
}
