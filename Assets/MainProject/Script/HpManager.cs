using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
public class HpManager : MonoBehaviour
{
    public static HpManager instance;

    public int nowHp=5;

    public Sprite good;
    public Sprite bad;

    public List<Image> hpImage = new List<Image>();

    private void Awake()
    {
        if(instance == null)instance = this;
    }

    public void GetHp(int hp)
    {
        if (nowHp < 5 && hp >= 0) nowHp += hp;
        else if (nowHp < 0 && hp < 0) nowHp += hp;
        SetHpImg();
    }

    private void SetHpImg()
    {
        for(int i = 0;i<5; i++)
        {
            if (i < nowHp) hpImage[i].sprite = good;
            else hpImage[i].sprite = bad;
        }
    }
}
