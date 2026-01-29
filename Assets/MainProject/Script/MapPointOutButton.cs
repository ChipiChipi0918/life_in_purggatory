using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapPointOutButton : MonoBehaviour
{
    public string placeName;

    public void SelectPlace()
    {
        SoundManager.instance.UiSelect();
        ArgumentManager.instance.OnPlaceClicked(placeName);
    }
}
