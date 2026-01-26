using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapPointOutButton : MonoBehaviour
{
    public string placeName;

    public void SelectPlace()
    {
        ArgumentManager.instance.OnPlaceClicked(placeName);
    }
}
