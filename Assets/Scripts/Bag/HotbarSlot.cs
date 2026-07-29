using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class HotbarSlot
{
    public GameObject item;
    public int count;
    public Image image;
    public TMP_Text countText;
}
