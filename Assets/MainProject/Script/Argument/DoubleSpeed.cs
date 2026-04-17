using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoubleSpeed : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject doubleSpeedUi;
    public GameObject speedLine1;
    public GameObject speedLine2;

    [Header("Settings")]
    public  int FRAME_INTERVAL = 26; // 스킵 속도 조절용 프레임 간격
    private int frameCount = 0;

    // DoubleSpeed.cs 수정본
    void Update()
    {
        // 1. UI 애니메이션 중일 때는 DoubleSpeed 로직을 완전히 중단합니다.
        // 여기서 ResetSpeed()를 호출하면 그 안의 Time.timeScale = 1 때문에 
        // UiManager의 0.01 설정이 무시됩니다.
        if (UiManager.instance.isUiAnim || UiManager.instance.isHotelInformation || UiManager.instance.isLogue)
        {
            // 시각적인 UI만 끄고, Time.timeScale은 건드리지 않고 리턴합니다.
            doubleSpeedUi.SetActive(false);
            speedLine1.SetActive(false);
            speedLine2.SetActive(false);
            return;
        }

        // 2. Ctrl 키 입력 처리
        if (Input.GetKey(KeyCode.LeftControl))
        {
            ProcessSpeedUp();
        }
        else
        {
            ResetSpeed(); // 여기서 비로소 평상시 scale 1로 복구
        }
    }

    private void ProcessSpeedUp()
    {
        var am = ArgumentManager.instance;

        // 1. 종료 대기 상태이거나 선택지 상태라면 스킵 방지
        if (am.CurrentState == ArgumentManager.FlowState.Argument_EndWait ||
            am.CurrentState == ArgumentManager.FlowState.Choice ||
            am.CurrentState == ArgumentManager.FlowState.Idle ||
            am.CurrentState == ArgumentManager.FlowState.PlaceSelection ||
            ArgumentManager.instance.isArgumentWrongFeedback || ArgumentManager.instance.isChoiceShowingWrongFeedback || ArgumentManager.instance.isMapPointOutShowingWrongFeedback)
        {
            ResetSpeed();
            return;
        }

        // 2. 논의 모드 (Argument Mode) -> 타임스케일 2배
        if (am.IsArgumentMode)
        {
            SetTimeScale(2.0f);
            ToggleVisuals(isArgument: true);
        }
        // 3. 일반 대화 모드 (Normal Dialogue) -> 빠르게 넘기기
        else
        {
            SetTimeScale(2.0f);

            ToggleVisuals(isArgument: false);

            frameCount++;
            if (frameCount >= FRAME_INTERVAL)
            {
                frameCount = 0;
                am.ForceSkip(); // ArgumentManager에 추가한 메서드 호출
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

        // 논의(Argument) 중에만 집중선 효과 켜기
        if (isArgument)
        {
            speedLine1.SetActive(true);
            speedLine2.SetActive(true);
        }
        else
        {
            speedLine1.SetActive(false);
            speedLine2.SetActive(false);
        }
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

        frameCount = 0; // 프레임 카운트 초기화
    }
}