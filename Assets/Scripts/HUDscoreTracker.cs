﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class HUDscoreTracker : MonoBehaviour
{
    internal int score = 0;
    private GUIStyle guiFontStyle = new GUIStyle();
    public TextMeshProUGUI coinText;

    public static HUDscoreTracker instance = null;


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else {
            Destroy(gameObject);
        }
    }

    public void addScore(int value)
    {
        score += value;
        coinText.text = score.ToString();
    }

    void Start()
    {
        //guiFontStyle.fontSize = 48;
        //guiFontStyle.normal.textColor = Color.green;
    }    

    void OnGUI()
    {
        //GUI.Label(new Rect(1200, 50, 200, 100), score.ToString(), guiFontStyle);
    }
}
