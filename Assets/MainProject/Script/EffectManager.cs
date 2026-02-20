using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EffectManager : MonoBehaviour
{
    public static EffectManager instance;

    [Header("Shake")]
    public bool usingShake = false;
    public Animator camAnim;

    [Header("피 2/3")]
    public GameObject blood;

    [Header("이의제기(논파) 11~20")]
    public RectTransform objection;
    public Image characterImage;
    public List<Sprite> objectionCharacter = new List<Sprite>();

    [Header("페이드 인아웃 100/101")]
    public GameObject fade;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    public void CameraShake() //1
    {
        usingShake = true;
        StartCoroutine(Shake()); // 코루틴 실행
    }
    IEnumerator Shake()
    {
        camAnim.SetTrigger("Shake");
        yield return new WaitForSecondsRealtime(0.1f);
        usingShake = false;
    }

    public void Blood() //2
    {
        StartCoroutine(BloodEffect());
    }

    private IEnumerator BloodEffect()
    {
        CanvasGroup bloodCanvasGroup = blood.GetComponent<CanvasGroup>();

        bloodCanvasGroup.alpha = 0f;
        blood.SetActive(true);

        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            bloodCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t / 0.5f);
            yield return null;
        }
        bloodCanvasGroup.alpha = 1f;

        t = 0f;
        while (t < 3f)
        {
            t += Time.deltaTime;
            bloodCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t / 3f);
            yield return null;
        }
        bloodCanvasGroup.alpha = 0f;
    }

    public void ShakeAndBlood() //3
    {
        EffectManager.instance.CameraShake();
        StartCoroutine(BloodEffect());
    }

    public void Objection(int characterNumber) //11~20
    {
        StartCoroutine(ObjectionCoroutine());

        characterImage.sprite = objectionCharacter[characterNumber];
    }

    IEnumerator ObjectionCoroutine()
    {
        CanvasGroup fadeCanvasGroup = objection.gameObject.GetComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 1f;
        float t = 0f;

        UiManager.instance.isUiAnim = true;
        objection.DOAnchorPosY(0, 0.2f);
        yield return new WaitForSeconds(2);
        
        while (t < 2f)
        {
            t += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, 1 - (t / 0.5f));
            if(t>=1.2f && t<=1.3f) UiManager.instance.isUiAnim = false;
            yield return null;
        }
        fadeCanvasGroup.alpha = 0f;

        yield return new WaitForSeconds(1f);
        objection.DOAnchorPosY(1222.426f, 0.01f);
        
    }

    public void FadeIn() //100
    {
        StopCoroutine("Fade");
        StartCoroutine(Fade(true));
    }

    public void FadeOut() //101
    {
        StopCoroutine("Fade");
        StartCoroutine(Fade(false));
    }

    private IEnumerator Fade(bool InOrOut)
    {
        CanvasGroup fadeCanvasGroup = fade.GetComponent<CanvasGroup>();

        fadeCanvasGroup.alpha = 0f;
        fade.SetActive(true);

        if (InOrOut)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t / 0.5f);
                yield return null;
            }
            fadeCanvasGroup.alpha = 1f;
        }
        else
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f,1 - ( t / 0.5f));
                yield return null;
            }
            fadeCanvasGroup.alpha = 0f;
        }
    }
}
