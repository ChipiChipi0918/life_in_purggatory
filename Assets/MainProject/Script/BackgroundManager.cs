using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundManager : MonoBehaviour
{
    public static BackgroundManager instance;

    public GameObject tribunalBackground;
    public GameObject nomalBackground;

    public List<Sprite> background = new List<Sprite>();
    private void Awake()
    {
        if (instance == null) instance = this;
    }

    private void Update()
    {
        if(DialogueFlowManager.instance.currentPhase == DialogueFlowManager.Phase.Daily)
        {
            nomalBackground.SetActive(true);
            tribunalBackground.SetActive(false);
        }
        else if(DialogueFlowManager.instance.currentPhase == DialogueFlowManager.Phase.Judgment)
        {
            nomalBackground.SetActive(false);
            tribunalBackground.SetActive(true);
        }
    }

    public void DailyMapUpdate(string backgroundName)
    {
        //background[0] 은 가독성을 위해 따로 할당 하지 않음

        if (backgroundName == "Black")
            nomalBackground.GetComponent<SpriteRenderer>().sprite = background[1];
        else if(backgroundName == "Tribunal")
            nomalBackground.GetComponent<SpriteRenderer>().sprite = background[2];
        else if (backgroundName == "Loby")
            nomalBackground.GetComponent<SpriteRenderer>().sprite = background[3];
        else if (backgroundName == "Yard")
            nomalBackground.GetComponent<SpriteRenderer>().sprite = background[4];
        else if (backgroundName == "Terrace")
            nomalBackground.GetComponent<SpriteRenderer>().sprite = background[5];
        else if (backgroundName == "Corridor")
            nomalBackground.GetComponent<SpriteRenderer>().sprite = background[6];
        else if (backgroundName == "Room")
            nomalBackground.GetComponent<SpriteRenderer>().sprite = background[7];
        else if (backgroundName == "Restaurant")
            nomalBackground.GetComponent<SpriteRenderer>().sprite = background[8];
    }
}
