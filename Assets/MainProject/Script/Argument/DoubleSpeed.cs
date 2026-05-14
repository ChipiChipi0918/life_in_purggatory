using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class DoubleSpeed : MonoBehaviour
{
    public static DoubleSpeed instance;

    [Header("UI Components")]
    public GameObject doubleSpeedUi;
    public GameObject speedLine1;
    public GameObject speedLine2;

    [Header("Settings")]
    public int FRAME_INTERVAL = 26;
    private int frameCount = 0;

    [Header("Stop")]
    public GameObject stopUi;
    public bool isStop;

    private void Awake()
    {
        if(instance == null) instance = this;
    }
    void Update()
    {
        // 1. UI 애니메이션 중일 때 예외 처리
        if (UiManager.instance.isUiAnim || UiManager.instance.isHotelInformation || UiManager.instance.isLogue)
        {
            doubleSpeedUi.SetActive(false);
            speedLine1.SetActive(false);
            speedLine2.SetActive(false);
            return;
        }

        // 2. 스페이스바 입력 처리 (일시정지 토글)
        if (Input.GetKeyDown(KeyCode.Space) && ArgumentManager.instance.IsArgumentMode)
        {
            isStop = !isStop;
            stopUi.SetActive(isStop);
        }

        // 3. 상태에 따른 속도 제어 (우선순위: 정지 > 2배속 > 일반)
        if (isStop && ArgumentManager.instance.IsArgumentMode)
        {
            // 정지 상태일 때
            SetTimeScale(0f);
            doubleSpeedUi.SetActive(false); // 정지 중에는 배속 UI는 끔
            speedLine1.SetActive(false);
            speedLine2.SetActive(false);
        }
        else if (Input.GetKey(KeyCode.LeftControl))
        {
            // 2배속/스킵 진행 중일 때
            ProcessSpeedUp();
        }
        else
        {
            // 평상시
            ResetSpeed();
        }
    }

    private void ProcessSpeedUp()
    {
        var am = ArgumentManager.instance;

        // 종료 대기, 선택지 등 스킵 방지 조건
        if (am.CurrentState == ArgumentManager.FlowState.Argument_EndWait ||
            am.CurrentState == ArgumentManager.FlowState.Choice ||
            am.CurrentState == ArgumentManager.FlowState.Idle ||
            am.CurrentState == ArgumentManager.FlowState.PlaceSelection ||
            am.isArgumentWrongFeedback || am.isChoiceShowingWrongFeedback || am.isMapPointOutShowingWrongFeedback)
        {
            ResetSpeed();
            return;
        }

        // 논의 모드 (배속만 적용)
        if (am.IsArgumentMode)
        {
            SetTimeScale(2.0f);
            ToggleVisuals(isArgument: true);
        }
        // 일반 대화 모드 (ForceSkip 적용)
        else
        {
            SetTimeScale(2.0f);
            ToggleVisuals(isArgument: false);

            frameCount++;
            if (frameCount >= FRAME_INTERVAL)
            {
                frameCount = 0;
                am.ForceSkip();
            }
        }
    }

    private void SetTimeScale(float scale)
    {
        if (Time.timeScale != scale)
        {
            Time.timeScale = scale;
        }
    }

    private void ToggleVisuals(bool isArgument)
    {
        doubleSpeedUi.SetActive(true);
        speedLine1.SetActive(isArgument);
        speedLine2.SetActive(isArgument);
    }

    private void ResetSpeed()
    {
        doubleSpeedUi.SetActive(false);
        speedLine1.SetActive(false);
        speedLine2.SetActive(false);

        if (Time.timeScale != 1.0f)
        {
            Time.timeScale = 1.0f;
        }
        frameCount = 0;
    }
}