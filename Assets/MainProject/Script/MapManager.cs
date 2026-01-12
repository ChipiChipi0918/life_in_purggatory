using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    [Header("Ãþ")]
    public List<GameObject> floor = new List<GameObject>();

    public void OnFlor(int f)
    {
        for(int i = 0; i < 4; i++)
        {
            floor[i].SetActive(false);
        }

        floor[f].gameObject.SetActive(true);
    }
}
