using UnityEngine;
using TMPro;
using System;

public class ClockSystem : MonoBehaviour
{
    public TextMeshProUGUI clockText; // ← フィールド名を合わせる

    void Update()
    {
        clockText.text = DateTime.Now.ToString("HH:mm");
    }
}