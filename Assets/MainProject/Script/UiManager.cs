using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UiManager : MonoBehaviour
{
    public static UiManager instance;

    public bool isUiAnim;
    public Transform camTransform;


    [Header("팝업 UI 관리 UI들")]
    public RectTransform popupUiOnPos;
    private bool isPopupUiOn = false;

    public GameObject currentNameUi;
    public TextMeshProUGUI currentNameUiText;
    public GameObject closeUi;

    [Header("논의 시작 시 나오는 Ui")]
    public RectTransform argumentEvidenceUi;
    public RectTransform argumentActUi;
    public RectTransform argumentTimeUi;

    [Header("Argument Confirm UI")]
    public RectTransform argumentConfirmPanel; // 확인 UI 패널
    public bool isConfirmUi;

    [Header("Back Ui")]
    public CanvasGroup backUi;

    [Header("로그")] //f1
    public GameObject logue;
    public bool isLogue;

    [Header("Hotel information")] //f2
    public RectTransform hotelInformation;
    public bool isHotelInformation;

    [Header("Ui Off")] //f3
    public GameObject allUi;
    public bool isUiOff=true;

    [Header("맵 지적")]
    public RectTransform mapPointOut;
    public bool isMapPointOut;

    [Header("논의 시작/종료")]
    public Transform argumentStartUi;
    public Transform argumentEndUi;

    public RectTransform argumentLineUp;
    public RectTransform argumentLineDown;


    private enum PopupType { None, Logue, HotelInformation, MapPointOut }
    private PopupType currentPopup = PopupType.None;

    private void Awake()
    {
        if (instance == null) instance = this;

        argumentStartUi.localScale = new Vector3(1, 0, 1);
        argumentEndUi.localScale = new Vector3(1, 0, 1);
    }

#region 팝업Ui

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            On_Logue();
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            On_HotelInformation();
        }

        if (Input.GetKeyDown(KeyCode.F3) || ((isUiOff && Input.GetMouseButtonDown(0)) || (isUiOff && Input.GetKeyDown(KeyCode.F3))))
        {
            On_OffUi();
        }
    }

    public void PopupButtonsOn()
    {
        if(isPopupUiOn)
            popupUiOnPos.DOAnchorPosX(124,0.7f).SetUpdate(false);
        else
            popupUiOnPos.DOAnchorPosX(-343, 0.7f).SetUpdate(false);

        isPopupUiOn = !isPopupUiOn;
    }

    public void On_Logue()
    {
        SoundManager.instance.UiSelect();

        if (!isLogue && !isUiAnim && !isHotelInformation)
        {
            isLogue = true;
            currentPopup = PopupType.Logue;
            PopupUiOn("Logue");
            StartCoroutine(LogueCoroutine(true));
        }
        else PopupUiOff();
    }
    public void On_HotelInformation()
    {
        SoundManager.instance.UiSelect();

        if (!isHotelInformation && !isUiAnim && !isLogue)
        {
            isHotelInformation = true;
            currentPopup = PopupType.HotelInformation;
            PopupUiOn("Inform");
            StartCoroutine(HotelInformationCoroutine(true));
        }
        else PopupUiOff();
    }

    public void On_OffUi()
    {
        SoundManager.instance.UiSelect();

        if (!isUiOff)
        {
            isUiOff = true;
            allUi.SetActive(false);
        }
        else
        {
            isUiOff = false;
            allUi.SetActive(true);
        }
    }

    public void PopupUiOn(string uiName)
    {
        currentNameUiText.text = uiName;
        currentNameUi.SetActive(true);
        closeUi.SetActive(true);
    }

    public void PopupUiOff()
    {
        if (isUiAnim) return;

        switch (currentPopup)
        {
            case PopupType.Logue:
                isLogue = false;
                StartCoroutine(LogueCoroutine(false));
                break;

            case PopupType.HotelInformation:
                isHotelInformation = false;
                StartCoroutine(HotelInformationCoroutine(false));
                break;

            case PopupType.MapPointOut:
                isMapPointOut = false;
                StartCoroutine(MapPointOutUiCoroutine(false));
                break;
        }

        SoundManager.instance.UiSelect();

        currentPopup = PopupType.None;
        currentNameUi.SetActive(false);
        closeUi.SetActive(false);
    }
#endregion

    private IEnumerator BackUiFade(bool on)
    {
        backUi.alpha = on ? 0f : 1f;
        backUi.gameObject.SetActive(true);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime;
            backUi.alpha = on ? Mathf.Lerp(0f, 1f, t / 0.5f) : Mathf.Lerp(1f, 0f, t / 0.5f);
            yield return null;
        }

        backUi.alpha = on ? 1f : 0f;
    }

    private IEnumerator HotelInformationCoroutine(bool on)
    {
        isUiAnim = true;

        StartCoroutine(BackUiFade(on));
        Time.timeScale = on ? 0 : 1;

        if (on)
        {
            hotelInformation.gameObject.SetActive(true);
            hotelInformation.DOAnchorPosY(0, 0.6f).SetUpdate(true);
            yield return new WaitForSecondsRealtime(0.6f);
        }
        else
        {
            hotelInformation.DOAnchorPosY(-1490, 0.6f).SetUpdate(true);
            yield return new WaitForSecondsRealtime(0.6f);
            hotelInformation.gameObject.SetActive(false);
        }

        isUiAnim = false;
    }

    private IEnumerator LogueCoroutine(bool on)
    {
        isUiAnim = true;

        StartCoroutine(BackUiFade(on));
        Time.timeScale = on ? 0 : 1;
        logue.SetActive(on);

        yield return new WaitForSecondsRealtime(1f);
        isUiAnim = false;
    }

    public void MapPointOutUiToggle()
    {
        if (isUiAnim) return;

        isMapPointOut = !isMapPointOut;
        currentPopup = PopupType.MapPointOut;

        StartCoroutine(MapPointOutUiCoroutine(isMapPointOut));
    }

    private IEnumerator MapPointOutUiCoroutine(bool on)
    {
        isUiAnim = true;

        StartCoroutine(BackUiFade(on));
        Time.timeScale = on ? 0 : 1;

        mapPointOut.DOAnchorPosY(on ? 0 : -1490, 1).SetUpdate(true);
        yield return new WaitForSecondsRealtime(1f);

        isUiAnim = false;
    }
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

    // UiManager 내부 함수 수정 제안
    public void ShowArgumentConfirmUi(bool on)
    {
        // 이미 애니메이션 중이면 무시 (중복 클릭 방지)
        if (on && isUiAnim) return;

        isUiAnim = true; // 애니메이션 시작
        StartCoroutine(BackUiFade(on));

        if (on)
        {
            argumentConfirmPanel.gameObject.SetActive(true);
            argumentConfirmPanel.localScale = Vector3.zero;
            argumentConfirmPanel.DOScale(1, 0.4f).SetEase(Ease.OutBack).SetUpdate(true)
                .OnComplete(() => isUiAnim = false); // 🔥 완료 후 해제
        }
        else
        {
            argumentConfirmPanel.DOScale(0, 0.3f).SetEase(Ease.InBack).SetUpdate(true)
                .OnComplete(() => {
                    argumentConfirmPanel.gameObject.SetActive(false);
                    isUiAnim = false; // 🔥 완료 후 해제
                });
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

    
}
