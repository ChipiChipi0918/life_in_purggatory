using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HotelInformationManager : MonoBehaviour
{
    static HotelInformationManager instance;

    public int informationTap = 0;

    public TextMeshProUGUI page;

    public TextMeshProUGUI bookmark;

    [Header("ÅÇ")]
    public GameObject evidenceTap; //0
    public GameObject guestTap; //1
    public GameObject mapTap; //2
    public GameObject ruleTap;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    private void InformationTapUpdate()
    {
        evidenceTap.SetActive(false);
        guestTap.SetActive(false);
        mapTap.SetActive(false);
        ruleTap.SetActive(false);

        if (informationTap < 0)
            informationTap = 2;
        else if (informationTap > 3)
            informationTap = 0;


        if (informationTap == 0)
        {
            bookmark.text = "Áõ°ÅÇ°";
            evidenceTap.SetActive(true);
        }
        else if(informationTap == 1)
        {
            bookmark.text = "Åõ¼÷°´";
            guestTap.SetActive(true);
        }
        else if(informationTap == 2)
        {
            bookmark.text = "Áöµµ";
            mapTap.SetActive(true);
        }
        else if(informationTap == 3)
        {
            bookmark.text = "±ÔÄ¢";
            ruleTap.SetActive(true);
        }
        page.text = "Page." + (informationTap + 1);
    }

    public void LeftArrow()
    {
        informationTap -= 1;
        InformationTapUpdate();
    }

    public void RightArrow()
    {
        informationTap += 1;
        InformationTapUpdate();
    }
}
