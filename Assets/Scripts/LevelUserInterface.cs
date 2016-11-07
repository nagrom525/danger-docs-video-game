﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Serialization;
using System;

public class LevelUserInterface : MonoBehaviour {
	public Text heartrate;
	public Image EventSignal;
	public float doctorBlinkDuration;
	public static LevelUserInterface UI;

	void Awake() {
		// setting up singleton code
		if (UI == null) {
			UI = this;
		} else {
			Debug.Log("DoctorEvents can only be set once");
		}
	}

	// Use this for initialization
	void Start () {
        // We probably want to register private member functions with DoctorEvents delegates
        DoctorEvents.Instance.heartAttackGreenEvent += OnGreenHeartAttack;
        DoctorEvents.Instance.heartAttackBlueEvent += OnBlueHeartAttack;
        DoctorEvents.Instance.heartAttackRedEvent += OnRedHeartAttack;
        DoctorEvents.Instance.heartAttackOrangeEvent += OnOrangeHeartAttack;
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void UpdateBpm(float bpm) {
		heartrate.text = bpm.ToString () + " BPM";
	}

    // -- Listen for events -- //
    void OnBlueHeartAttack(float duration) {

    }

    void OnGreenHeartAttack(float duration) {

    }

    void OnRedHeartAttack(float duration) {

    }

    void OnOrangeHeartAttack(float duration) {

    }

}
