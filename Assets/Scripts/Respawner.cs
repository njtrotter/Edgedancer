﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Respawner : MonoBehaviour
{
    public Vector3 respawnPoint;
    private Quaternion respawnOrientation;
    public  TextMeshProUGUI deathCounterText;
    public TextMeshProUGUI deathMessageText;
    private string causeOfDeathMessage = "Nothing";
    private float deathMsgTimer = 2.0f;


    public float health = 50f;
    public float maxHealth = 50f;

    public GameObject healthBarUI;
    public Image healthBarImage;

    public static Respawner instance;

    private GUIStyle guiFontStyle = new GUIStyle();
	private int deathCount = 0;

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

    void Start()
	{
		guiFontStyle.fontSize = 32;
        guiFontStyle.normal.textColor = Color.red;
        healthBarImage.fillAmount = maxHealth / maxHealth;
        respawnOrientation = GetComponentInParent<Transform>().rotation;
    }
    public void playerRespawn(string causeOfDeath){
        //currently resets player to one standard spawn point
        health = maxHealth;
        healthBarImage.fillAmount = maxHealth / maxHealth;
        Transform playerTransform = GetComponentInParent<Transform>();
        playerTransform.position = respawnPoint;
        playerTransform.rotation = respawnOrientation;
        playerTransform.gameObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
        causeOfDeathMessage = "Player died from " + causeOfDeath;
        deathCount++;
        deathCounterText.text = deathCount.ToString();
        GameManager.instance.deathCounter++;
        StartCoroutine("DeathPrompt");
    }
    void OnGUI()
     {
		 //GUI.Label(new Rect(150, 35, 200, 100), "Death Count: " + deathCount.ToString(), guiFontStyle);
   //      //displays death message for 5 seconds in upper left of screen
   //      if(showDeathText){
   //         GUI.Label(new Rect(150, 65, 200, 100), causeOfDeathMessage, guiFontStyle);
   //         deathMsgTimer -= Time.deltaTime;
   //         if (deathMsgTimer < 0){
   //             deathMsgTimer = 5.0f;
   //             showDeathText = false;
   //         }
   //      }
            
     }

    public void TakeDamage(float amount)
    {
        healthBarUI.SetActive(true);
        health -= amount;
        healthBarImage.fillAmount = health / maxHealth;
        Debug.Log(health);
        if (health <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        playerRespawn("lazers");
    }


    IEnumerator DeathPrompt() {
        GameObject deathScreen = GameObject.Find("DeathScreen");
        deathMessageText.text = causeOfDeathMessage;
        GameManager.instance.isRespawning = true;

        while (deathScreen.GetComponent<CanvasGroup>().alpha < 1) {
            deathScreen.GetComponent<CanvasGroup>().alpha += Time.unscaledDeltaTime * 0.01f;
        }
        yield return new WaitForSecondsRealtime(deathMsgTimer);
        GameManager.instance.isRespawning = false;
        deathScreen.GetComponent<CanvasGroup>().alpha = 0.0f;
    }
}
