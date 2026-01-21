using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoubleSpeed : MonoBehaviour
{
    [Header("Ui")]
    public GameObject doubleSpeedUi;

    [Header("속도선")]
    public GameObject speedLine1;
    public GameObject speedLine2;

    [Header("프레임 카운트")]
    public const int FRAME_INTERVAL = 13;
    private int frameCount = 0;

    void Update()
    {
        HandleDoubleSpeed();

        if (UiManager.instance.isUiAnim)
        {
            Nomal();
        }
    }

    void HandleDoubleSpeed()
    {
        if (ArgumentManager.instance.waitingArgumentEndText) return;

        if (!Input.GetKey(KeyCode.LeftControl))
        {
            // Ctrl 안 누르면 정상 속도로 복귀
            Nomal();
            return;
        }

        if (ArgumentManager.instance.isArgumentActive)
        {
            // 🔥 여기서는 프레임 제한 없음
            if (!UiManager.instance.isUiAnim)
            {
                doubleSpeedUi.SetActive(true);
                speedLine1.SetActive(true);
                speedLine2.SetActive(true);
                Time.timeScale = 2;
            }
            else if (Time.timeScale == 2)
            {
                Nomal();
            }
        }
        else
        {
            frameCount++;

            if (frameCount % FRAME_INTERVAL != 0)
                return;

            if (!ArgumentManager.instance.isChoice)
            {
                doubleSpeedUi.SetActive(true);
                
                ArgumentManager.instance.PlayNext();
                ArgumentManager.instance.isSkipTyping = true;
            }
            

            if (Time.timeScale == 2)
            {
                Nomal();
            }
        }
    }

    void Nomal()
    {
        doubleSpeedUi.SetActive(false);
        if (Time.timeScale == 2)
        {
            speedLine1.SetActive(false);
            speedLine2.SetActive(false);
            Time.timeScale = 1;
        }
    }
}
