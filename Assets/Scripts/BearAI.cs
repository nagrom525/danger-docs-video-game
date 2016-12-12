﻿using UnityEngine;
using System.Collections;

public class BearAI : MonoBehaviour {

	BearAI B;
	public Transform PatientGurney;
	GameObject patient;
	NavMeshAgent agent;
	bool targetAcheived;
	public GameObject Cave;
	Vector3 startposition;
	public int push_back_threshold = 4;
	private int push_back_num = 0;
    public GameObject actionButtonCanvas;
    public static bool scaredAwayOnce = false;

	public float doctorDashTimerPadding = 1f;   //if all doctors dash the bear within a second of each other

	public Renderer bearRenderer;
	public Material defaultMat;
	public Material hitMat;
	public GameObject bearModel;

	public GameObject [] Circlearound;
	private int currpos = 0;
	public int MAX_POSITION_STANDS = 4;

	void OnEnable()
	{
		AudioControl.Instance.PlayBearEnter();
	}

	void Awake()
	{
		if (B == null)
		{
			B = this;
		} else 
		{
			Debug.Log("There is more than one bear on the screen");
		}
		bearRenderer = bearModel.GetComponent<Renderer>();
		defaultMat = bearRenderer.sharedMaterial;
	}

	// Use this for initialization
	void Start () {
		AudioControl.Instance.PlayBearEnter();
		//Circlearound = new GameObject[MAX_POSITION_STANDS];
		patient = Patient.Instance.gameObject;
		this.agent = GetComponent<NavMeshAgent>();
		this.agent.destination = patient.transform.position;
		this.gameObject.transform.LookAt(patient.transform.position);
		//targetAcheived = false;
		startposition = this.gameObject.transform.position;
        if (scaredAwayOnce) {
            actionButtonCanvas.SetActive(false);
        } else {
			actionButtonCanvas.SetActive(true);
        }
	}

	void OnCollisionEnter(Collision other)
	{
		// bear stealing patient table
		//HACK: Super hacky way of doing this 
		if (other.transform.tag == "PatientTable" && other.gameObject.transform.parent.parent == null )
		{
			//print("how often do you happen?");
			//PatientGurney = other.gameObject.transform.parent.transform;
			makeParent(other);
			this.GetComponent<Rigidbody>().velocity = Vector3.zero;
			agent.Stop();
			BearMoveToNext();
			//BearSwitchToCave();
			AudioControl.Instance.PlayBearExit();
			DoctorEvents.Instance.InformBearStealingPatient();
		}
		else if (other.transform.tag == "Doctor")
		{
			//Debug.Log("bear-doctor collision");
			if (other.gameObject.GetComponent<Doctor>().justDashed)
			{
				push_back_num++;
				bearRenderer.material = hitMat;
				Invoke("ResetMaterial", .2f);
                if (TutorialEventController.Instance.tutorialActive) {
                    TutorialEventController.Instance.InfromPlayerScaredBear(other.gameObject.GetComponent<DoctorInputController>().playerNum);
                }
				
			}


			if (push_back_num >= push_back_threshold)
			{
				PatientGurney.parent = null;
				patient.transform.parent = null;
				BearSwitchToCave();
				actionButtonCanvas.SetActive(false);
				this.gameObject.GetComponent<Rigidbody>().velocity = Vector3.zero;

			}

			Invoke("ResetThreshold", doctorDashTimerPadding);
		}
		
	}

	void ResetThreshold()
	{
		push_back_num = 0;
	}

	void OnTriggerEnter(Collider other)
	{
		//print("trigger anything");
		if (other.transform.tag == "Cave")
		{
			//print("why are you not triggering");
			this.gameObject.SetActive(false);
			if (patient.transform.parent != null)
			{
				patient.SetActive(false);
				DoctorEvents.Instance.InducePatientDeath();
			}
			BearInCave();
			this.gameObject.transform.position = startposition;

		} else if (other.transform.tag == "NextPositionStand")
			//HACK: Super ridiculous I apologize
		{
			Debug.Log("we got here");
			//Debug.Log("is this true? " + (other.gameObject == Circlearound[currpos]));
			Debug.Log("what is the current position?" + currpos);
			if (currpos < MAX_POSITION_STANDS && other.gameObject.transform == Circlearound[currpos].transform)
			{
				this.GetComponent<Rigidbody>().velocity = Vector3.zero;
				agent.Stop();
				Debug.Log("now let's go");
				currpos++;
				BearMoveToNext();
			}
			else {
				this.GetComponent<Rigidbody>().velocity = Vector3.zero;
				agent.Stop();
				BearMoveToNext();
			}
		}

	}

	void makeParent(Collision other)
	{
		PatientGurney.parent = this.gameObject.transform;
		patient.transform.parent = this.gameObject.transform;
	}

	void BearMoveToNext()
	{
		//print("it's happening");

		if (currpos < MAX_POSITION_STANDS)
		{
			print("you should only happen once");
			//this.GetComponent<Rigidbody>().velocity = Vector3.zero;
			//agent.Stop();
			agent.destination = Circlearound[currpos].transform.position;
			agent.Resume();
		} 
		else 
		{
			BearSwitchToCave();
		}
	}

	void BearSwitchToCave()
	{
		//AudioControl.Instance.PlayBearExit();
		//this.GetComponent<Rigidbody>().velocity = Vector3.zero;
		//agent.Stop();
		agent.destination = Cave.transform.position;
		//AudioControl.Instance.PlayBearExit();
		agent.Resume();

	}

	void ResetMaterial()
	{
		bearRenderer.material = defaultMat; 
	}

	void BearInCave()
	{
		if (PatientGurney.parent != null)
		{
			PatientGurney.parent = null;
			patient.transform.parent = null;
		}
		AudioControl.Instance.PlayBearExit();
		DoctorEvents.Instance.InformBearLeft();

	}

	// Update is called once per frame
	//void Update()
	//{
	//	this.GetComponent<Rigidbody>().velocity = Vector3.zero;
	//	//agent.destination = patient.transform.position;
	//	//if(targetAcheived) agent.Stop();
	//}

}
 