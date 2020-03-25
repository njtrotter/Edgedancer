﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Triggers : MonoBehaviour
{

    [Tooltip("Only Check one bool here according to the type of behaviour you want associated with it")]

    public bool m_Door;
    public Animator doorAnimator;
    [Header("------------------------------------------------")]
    public bool m_Tutorial;
    public GameObject tutorialText;
    [Header("Triggers For Control Activation")]
    public bool m_CanJump;
    public bool m_CanRun;
    public bool m_CanGrapple;
    public bool m_CanShoot;
    [Header("------------------------------------------------")]
    public bool tutorialComplete;
    public bool level1Start;
    public bool level1end;
    public bool level2Start;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (m_Door)
            {
                doorAnimator.SetTrigger("DoorOpen");
                this.transform.parent.gameObject.GetComponent<AudioSource>().PlayOneShot(AudioManager.instance.doorAudio);
            }
            if (m_Tutorial)
            {
                tutorialText.SetActive(true);
            }
            if (m_CanJump) { GameManager.instance.canJump = true; }
            if (m_CanRun) { GameManager.instance.canRun = true; }
            if (m_CanGrapple) { 
                GameManager.instance.canGrapple = true;
                GameObject.Find("Player Body").GetComponent<AudioSource>().PlayOneShot(AudioManager.instance.grappleReloaded);
            }
            if (m_CanShoot) { GameManager.instance.canShoot = true; }

            if (tutorialComplete) {
                GameManager.instance.currentLevel = GameManager.CurrentLevel.connector1;
                Respawner.instance.respawnPoint = GameManager.instance.respawnConnector1;
            }
            if (level1Start) {
                GameManager.instance.currentLevel = GameManager.CurrentLevel.level1;
                Respawner.instance.respawnPoint = GameManager.instance.respawnLevel1;
            }
            if (level1end) {
                GameManager.instance.currentLevel = GameManager.CurrentLevel.connector2;
            }
            if (level2Start) {
                GameManager.instance.currentLevel = GameManager.CurrentLevel.level2;
                Respawner.instance.respawnPoint = GameManager.instance.respawnLevel2;
            }
        }
    }
}