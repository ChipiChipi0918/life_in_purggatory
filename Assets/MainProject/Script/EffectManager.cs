using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectManager : MonoBehaviour
{
    public static EffectManager instance;

    public GameObject fade;

    public GameObject blood;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    public void CameraShake() //1
    {
        UiManager.instance.Shaking(0.7f);
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
        UiManager.instance.Shaking(0.3f);
        StartCoroutine(BloodEffect());
    }

    public void FadeIn() //100
    {
        
    }

    public void FadOut() //101
    {

    }

    private IEnumerator Fade(bool InOrOut)
    {
        CanvasGroup fadeCanvasGroup = fade.GetComponent<CanvasGroup>();

        fadeCanvasGroup.alpha = 0f;
        fade.SetActive(true);

        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t / 0.5f);
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;
    }
}
