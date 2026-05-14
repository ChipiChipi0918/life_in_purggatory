using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TutorialStep
{
    public string stepName; // 에디터 식별용 이름
    public List<GameObject> objectsToActivate; // 이 단계에서 켜질 오브젝트들
}

public class ArgumentTutorial : MonoBehaviour
{
    public static ArgumentTutorial instance;

    [Header("튜토리얼 단계 설정")]
    public List<TutorialStep> tutorialSteps = new List<TutorialStep>();

    [Header("논의 버튼")]
    public GameObject argumentActButtonCounterarument;
    public GameObject argumentActButtonArgument;
    public GameObject argumentActButtonPerjury;

    private void Awake()
    {
        if(instance == null) instance = this;
    }
    private void Start()
    {
        TutorialOff(0);
    }
    public void TutorialOn(int argumentNumber)
    {
        if (argumentNumber < 0 || argumentNumber >= tutorialSteps.Count) return;

        // 해당 단계에 등록된 모든 오브젝트를 순회하며 활성화
        foreach (GameObject obj in tutorialSteps[argumentNumber].objectsToActivate)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }

        if (argumentNumber == 0)
        {
            argumentActButtonCounterarument.SetActive(true);
            argumentActButtonArgument.SetActive(false);
            argumentActButtonPerjury.SetActive(false);
        }

        if (argumentNumber == 2)
        {
            argumentActButtonArgument.SetActive(true);
        }
    }

    /// <summary>
    /// 특정 단계의 모든 오브젝트를 비활성화합니다.
    /// </summary>
    public void TutorialOff(int argumentNumber)
    {
        if (argumentNumber < 0 || argumentNumber >= tutorialSteps.Count) return;

        foreach (GameObject obj in tutorialSteps[argumentNumber].objectsToActivate)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
    }
}