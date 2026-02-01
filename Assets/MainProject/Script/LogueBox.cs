using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LogueBox : MonoBehaviour
{
    public GameObject nameTagObj;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;
    
    public void LogueBoxUpdate(string name,string text)
    {
        dialogueText.text = text;

        if (string.IsNullOrEmpty(name))
        {
            nameTagObj.SetActive(false);
            return;
        }

        nameText.text = name;

        string colorCode = DialogueDirector.instance.characterConfig.ContainsKey(name) ? DialogueDirector.instance.characterConfig[name].colorCode : DialogueDirector.instance.characterConfig["Default"].colorCode;

        string firstChar = name.Substring(0, 1);
        string restName = name.Length > 1 ? name.Substring(1) : "";

        nameText.text = $"<size=180%><color={colorCode}>{firstChar}</color></size>{restName}";
    }
}
