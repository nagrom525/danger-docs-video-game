﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Doctor : MonoBehaviour {
    // currentTool can only be set by the Doctor when it interacts with a tool
    public Tool currentTool {get; private set;}

	public float dirtLevel;
	public ParticleSystem dirtPS;
	public bool interacting;
    private bool inSurgery = false;
	public bool dirtyHands {
		get { return dirtLevel > 0f; }
	}

	public Image washingMeter;
	public Image Bar;
	private int washingMeterFramesRemaining;

	public Material hlMat;
    public Material rechargeMaterial;
    private Material normalMaterial;
	private Tool last_hl_tool;
	private Tool current_hl_tool;
	private Material original_go_material;
	private Vector3 checkOffset;
	private Vector3 interactionBoxHalfExtents;
	private float drSpeedCoefficient;
    private SurgeryToolInput surgeryInput = null;

	private Rigidbody docRB;

	public GameObject fireParticles;
	//Dash variables
	public bool 		canDash = true;
	public float 		dashSpeed = 2f;
	public float 		dashDelay = 2f;
	public float 		timeBetweenDashes = 4f;
	public bool 		justDashed;
	public GameObject 	dustParticlePrefab;
    public GameObject dashColorChanger;
    private Renderer doctorRenderer;

	// Radius of sphere for checking for interactiables.
	private float interactionRange = 8f;
    private bool animatingWaterMeter = false;
    private float waterMeterStartTime = 0.0f;
    private float waterMeterTotalTime = 0.0f;
    private float waterMeterEndValue = 0.0f;
    private float waterMeterStartValue = 0.0f;
    public float waterMeterDepleatPercentPerSecond = 100.0f;

	public int onFireFrames;
	private Vector3 onFireDir;
    private bool tutorialSurgeryStepComplete = false;

	// Use this for initialization
	void Start () {
		currentTool = null;
        // TODO: NEED to register event listner functions to the DoctorEvents singleton delegates
        TutorialEventController.Instance.OnSurgeryOnPatientEnd += OnTutorialSurgeryStepComplete;

        // Hands start out dirty.
        dirtLevel = 1f;
		fireParticles = transform.Find("Fire Particles").gameObject;

		interacting = false;

		Image[] objects = transform.GetComponentsInChildren<Image>();

        doctorRenderer = dashColorChanger.GetComponent<Renderer>();
        normalMaterial = doctorRenderer.material;

		washingMeter = objects[1];
		washingMeter.enabled = false;
		Bar = objects[0];
		Bar.enabled = false;
		washingMeterFramesRemaining = 0;
		interactionBoxHalfExtents = Vector3.one * 2.2f;

		onFireFrames = 0;

		drSpeedCoefficient = 10f;

		docRB = GetComponent<Rigidbody>();

		dirtPS.Play();
	}
	
	// Update is called once per frame
	void Update () {
		if (washingMeterFramesRemaining > 0) {
			updateWashingMeter ();
		} else {
			hideWashingMeter ();
		}
        if (animatingWaterMeter) {
            float t = (Time.time - waterMeterStartTime) / waterMeterTotalTime;
            if(t >= 1.0) {
                animatingWaterMeter = false;
                dirtLevel = waterMeterEndValue;
            } else {
                dirtLevel = Mathfx.Hermite(waterMeterStartValue, waterMeterEndValue, t);
            }
        }

		// Update highlighting system
		updateHighlights();

		// Update checkOffset
		checkOffset = transform.localRotation * (new Vector3(0, 0, 1) * 2.5f) + (Vector3.down * 4.5f);


		if (inSurgery)
		{
			//dont move
			docRB.velocity = Vector2.zero;
		}
	}

	void FixedUpdate() {
		if (onFireFrames > 0)
		{
			Vector3 xzNoise = new Vector3(
				Random.Range(0.5f, 0.7f),
				0f,
				Random.Range(0.5f, 0.7f)
			);
			OnJoystickMovement(onFireDir + xzNoise); // todo: add xz plane noise when running back.
													 //displayFireEffects();
			onFireFrames--;
		}
		else
		{
			fireParticles.SetActive(false);
		}
	}

	public void ignite() {
		Vector3 dir = transform.forward * -1f;
		print("ignited!");
		onFireFrames = 75;
		fireParticles.SetActive(true);
		onFireDir = new Vector3(dir.x, 0f, dir.z).normalized;
        // check to see if we need to end the surgery!
        if (inSurgery && (surgeryInput != null)) {
            surgeryInput.ReturnControlToDoctor();
        }
	}

	// Currently just handles highlighting tools.
	private void updateHighlights() {
		// If currently using a tool
		if (currentTool != null && currentTool.highlighted) {
			// make sure the tool isn't highlighted.
			currentTool.disableHighlighting();
			return;
		}
		highlightNearestTool();
	}

	// Please forgive me. I tried to make this intelligable.
	private void highlightNearestTool() {
		// Get nearest GO
		current_hl_tool = getNearestToolInRange (interactionRange);

		// If nearest object hasn't changed, there is nothing to be done
		if (current_hl_tool == last_hl_tool)
		{
			return;
		}
	
		// current_interactive_obj is new or null
		// Change old object back to original material.
		if (last_hl_tool != null) {
			// remove Highlighting
			last_hl_tool.disableHighlighting();
		}
		if (current_hl_tool != null) {
			// Highlight the current object
			current_hl_tool.enableHighlighting(hlMat);
		}
		// We then save current object as last object.
		last_hl_tool = current_hl_tool;
	}

	private GameObject getNearestGOwithTag(float range, string datTag) {
		// Get the interactables. Eventually, this should take a third agrument
		// (layer mask) which ignores everything that isn't an interactable.
		//Collider[] objectsInRange = Physics.OverlapSphere(pos, range);
		Collider[] objectsInRange = Physics.OverlapBox(pos + checkOffset, interactionBoxHalfExtents);
		Debug.DrawRay(pos, checkOffset);
		Debug.DrawRay(pos + checkOffset, Vector3.down);

		// Setup linear search for nearest interactable.
		GameObject nearestObj = null;
		float runningNearestObj = Mathf.Infinity;

		for (int i = 0; i < objectsInRange.Length; i++)
		{
			if (objectsInRange[i].gameObject.CompareTag(datTag))
			{
				Vector3 objPos = objectsInRange[i].transform.position;
				// Comparing sqrDistances is faster than mag. Avoids sqrt op.
				float sqrDist = (objPos - pos).sqrMagnitude;

				// If this interactable is closer than the current closest, update
				// to this one.
				if (runningNearestObj > sqrDist)
				{
					runningNearestObj = sqrDist;
					nearestObj = objectsInRange[i].gameObject;
				}
			}
		}

		return nearestObj;
	}

	// Logic for receiving joystick movement.
	// Moves the player according to the Vector3
	// recieved from input manager.
	public void OnJoystickMovement(Vector3 joystickVec) {
		joystickVec *= drSpeedCoefficient;
		transform.LookAt(transform.position + joystickVec);
		// We should never be moving in the z direction.
		//joystickVec.z = 0f;
		// Move in the direction of the joystick.
		//pos += joystickVec * Time.deltaTime;
		if (justDashed)
		{
			docRB.velocity = dashSpeed*joystickVec;
		}
		else 
		{
			docRB.velocity = joystickVec;
		}

	}

	public void OnPickupButtonPressed() {
		// If we currently have a tool, drop the tool.
		if (currentTool != null) {
			dropCurrentTool ();
			AudioControl.Instance.PlayToolDrop();
		} else {
			// If there is a tool in range, get that tool.
			// Otherwise, nearestTool == null
			Tool nearestTool = getNearestToolInRange(interactionRange);

			// If there is a nearby tool, equip it.
			if (nearestTool != null) {
				equipTool (nearestTool);
                nearestTool.OnDoctorInitatedInteracting();
                // special case for the bucket
                bool full = false;
                if(nearestTool.GetToolType() == Tool.ToolType.BUCKET) {
                    WaterBucket bucket = nearestTool.GetComponent<WaterBucket>();
                    full = bucket.hasWater;
                }
                DoctorEvents.Instance.InformToolPickedUp(nearestTool.GetToolType(), full);
                if (TutorialEventController.Instance.tutorialActive) {
                    TutorialEventController.Instance.InformToolPickedUp(nearestTool.GetToolType(), GetComponent<DoctorInputController>().playerNum);
                }
				AudioControl.Instance.PlayToolPickup();
			}
		}
        if (TutorialEventController.Instance.tutorialActive) {
            TutorialEventController.Instance.InformAButtonPressed(gameObject.GetComponent<DoctorInputController>().playerNum);
        }
	}

	private void equipTool(Tool tool) {
		currentTool = tool;
		tool.transform.parent = this.transform;
		// Transform tool position to doctor.
		tool.transform.localPosition = new Vector3 (1, 3, 0) * 0.3f;
        if (TutorialEventController.Instance.tutorialActive) {
            TutorialEventController.Instance.OnToolPickedUp(tool.GetToolType(), GetComponent<DoctorInputController>().playerNum);
        }
		Rigidbody rb = tool.transform.GetComponentInChildren<Rigidbody> ();
		if (rb != null) {
			// Add constraints
			rb.constraints = RigidbodyConstraints.FreezeAll;
			rb.useGravity = false;
			rb.isKinematic = true;
		}
	}

	private void dropCurrentTool() {
        if (TutorialEventController.Instance.tutorialActive) {
            TutorialEventController.Instance.OnToolDropped(currentTool.GetToolType(), GetComponent<DoctorInputController>().playerNum);
        }
		Rigidbody rb = currentTool.transform.GetComponentInChildren<Rigidbody> ();
		if (rb != null) {
			// Remove Constraints
			rb.constraints = RigidbodyConstraints.None;
			rb.useGravity = true;
			rb.isKinematic = false;
		}
        // special case for bucket
        bool full = false;
        if(currentTool.GetToolType() == Tool.ToolType.BUCKET) {
            WaterBucket bucket = currentTool.GetComponent<WaterBucket>();
            full = bucket.hasWater;
        }
        DoctorEvents.Instance.InformToolDropped(currentTool.GetToolType(), full);
        currentTool.transform.parent = null;
		currentTool = null;
	
	}

	private bool patientInRange() {
		float distToPatient = (Patient.Instance.transform.position - pos).magnitude;
		if (distToPatient <= interactionRange)
		{
			return true;
		}
		return false;
	}

	public void useCurrentToolOnPatient() {
		Debug.Log("useCurrentToolOnPatient triggered\nCurrent Tool: " + currentTool);
		if (dirtyHands && currentTool.GetToolType() != Tool.ToolType.DEFIBULATOR) {
            // Signal this somehow.
            DoctorEvents.Instance.InformDoctorNeedsToWashHands(0.0f);
            return;
        }

		if (Patient.Instance.transform.parent != null) {
			Debug.Log("Patient being carried off -- can't operate");
			return;
		}

		if (currentTool.GetToolType() != Tool.ToolType.DEFIBULATOR && !tutorialSurgeryStepComplete)
		{
			DoctorEvents.Instance.InformSurgeryOperation();
			inSurgery = true;
		}
		else
		{
			//play defibulator surge
			AudioControl.Instance.PlayDefibulatorSurge();
		}
        // Use current tool on patient.
		if (TutorialEventController.Instance.tutorialActive && currentTool!= null && currentTool.GetToolType() != Tool.ToolType.DEFIBULATOR) {
            TutorialEventController.Instance.InformDoctorAtPatient(GetComponent<DoctorInputController>().playerNum);
        }
        surgeryInput =  Patient.Instance.receiveOperation (currentTool, GetComponent<DoctorInputController>().playerNum);
		if(currentTool.GetToolType() != Tool.ToolType.DEFIBULATOR)
			currentTool.gameObject.SetActive(false);
	}


	// GameObject with tag "Tool" must have tool component.
	private Tool getNearestToolInRange (float range) {
		GameObject toolGO = getNearestGOwithTag(range, "Tool");
		if (toolGO) {
			return toolGO.gameObject.GetComponentInChildren<Tool>();
		}
		return null;
	}

	// When the interaction button is pressed, we must check to see if there
	// is and interactable nearby. If there is, then we send a message to
	// the interactable that the doctor is initiating an interaction. The
	// interactiable accepts the interaction message and acts on it only if
	// it is valid.
	public void OnInteractionButtonPressed() {

		// TODO: Move this
		if (currentTool && currentTool.GetToolType() == Tool.ToolType.BUCKET)
		{
			WaterBucket wb = currentTool as WaterBucket;
			if (wb.hasWater)
			{
				putOutFire(wb);
				// Return so that it doesn't also fill the water bucket up.
				return;
			}
		}

		// If near patient, use tool on patient.
		if (patientInRange() && (currentTool.GetToolType() != Tool.ToolType.CANISTER))
		{
			useCurrentToolOnPatient();
		}

		// Whether you are currently interacting or not,
		// we'll want the nearest interactable.

		Interactable nearbyInteractable = getNearestInteractableInRange(interactionRange);

		// If there is a nearby interactable, then begin interacting!
		if (nearbyInteractable != null) {
			// If currently interacting, pressing this button again will cancel the interaction.
			if (interacting) {
				nearbyInteractable.DoctorTerminatesInteracting (this);
				interacting = false;
			}
			// If not currently interacting, and ...
			// If successfully inintiated interacting, set interacting to true,
			// IF THE ACTION REQUIRES SUSTAINED INTERACTION OVER A TIME PERIOD.
			// Otherwise, false.
			interacting = nearbyInteractable.DocterIniatesInteracting (this);
		}
	}

	// Gets the nearest nearby interactable within sphere of radius "range".
	private Interactable getNearestInteractableInRange(float range) {

		// Get the interactables. Eventually, this should take a third agrument
		// (layer mask) which ignores everything that isn't an interactable.
		Collider[] interactablesInRange = Physics.OverlapBox(pos + checkOffset, interactionBoxHalfExtents);
		
		// Setup linear search for nearest interactable.
		Interactable nearestInteractable = null;
		float runningNearestDistance = Mathf.Infinity;

		for (int i = 0; i < interactablesInRange.Length; i++) {
			if (interactablesInRange[i].gameObject.CompareTag ("Interactable")) {
				// Get Vector3 between pos of doctor and interactable
				Vector3 interactablePos = interactablesInRange[i].transform.position;
				// Comparing sqrDistances is faster than mag. Avoids sqrt op.
				float sqrDist = (interactablePos - pos).sqrMagnitude;

				// If this interactable is closer than the current closest, update
				// to this one.
				if (runningNearestDistance > sqrDist) { 
					runningNearestDistance = sqrDist;
					nearestInteractable = interactablesInRange[i].gameObject.GetComponent<Interactable>();
				}
			}
		}

		return nearestInteractable;
	}
	
	public void putOutFire(WaterBucket wb) {
		// Check for fires in target area.
		Vector3 sphereOrigin = pos + checkOffset;
		Collider[] cols = Physics.OverlapSphere(sphereOrigin, wb.splashRadius);
		Debug.DrawRay (pos, checkOffset, Color.red, 0.2f);

		// Destroy fires.
		for (int i = 0; i < cols.Length; i++) {
			if (cols[i].GetComponentInChildren<Flame>()) {
				Destroy(cols[i].gameObject);
			}
		}

		// Deplete water level
		wb.pourWater(transform.forward);
		// Make hands dirty! Hard coding full dirt levels
		makeDirty(1f);
	}

	public void makeDirty (float addedDirt) {
		waterMeterEndValue = Mathf.Clamp(dirtLevel + addedDirt, 0f, 1f);
        waterMeterStartTime = Time.time;
        waterMeterStartValue = dirtLevel;
        animatingWaterMeter = true;
        waterMeterTotalTime = ((waterMeterEndValue - waterMeterStartValue) * 100) / waterMeterDepleatPercentPerSecond;

		displayWashingMeter ();

		if (dirtPS.isStopped) {
			dirtPS.Play();
		}
	}

	public void washHands(float washRate) {
		dirtLevel = Mathf.Clamp(dirtLevel - washRate, 0f, 1f);
		displayWashingMeter ();
		print ("dirtLevel ::" + dirtLevel);
		AudioControl.Instance.PlayWaterBucketFill();
		if (TutorialEventController.Instance.tutorialActive) {
			DoctorInputController thisInput = transform.GetComponentInChildren<DoctorInputController>();
			int this_player_num = thisInput.playerNum;
			TutorialEventController.Instance.InformWashingHands(1f - dirtLevel, this_player_num);
		}
		if (dirtLevel < (0f + Mathf.Epsilon)) {
			dirtPS.Stop();
		}
	}

	public void displayWashingMeter() {
		washingMeter.enabled = true;
		Bar.enabled = true;
		washingMeterFramesRemaining = 120;
	}

	private void updateWashingMeter() {
		washingMeterFramesRemaining--;
		washingMeter.fillAmount = 1f - dirtLevel;
       
	}

	private void hideWashingMeter() {
		washingMeter.enabled = false;
		Bar.enabled = false;
	}

	// For my convenience
	private Vector3 pos {
		get {
			return this.transform.position;
		}
		set {
			this.transform.position = value;
		}
	}

	/// <summary>
	/// Dash the doctor in the direction it's facing
	/// </summary>
	public void Dash()
	{
		if (!canDash)
			return;

		//create puff particles
		CreateDustPlooms();
		AudioControl.Instance.PlayDoctorDash();
		docRB.velocity = Vector2.zero;
		docRB.velocity += dashSpeed * transform.forward;
		justDashed = true;
		canDash = false;
		Invoke("ResetDash", dashDelay);
		Invoke("AllowDash", timeBetweenDashes);
	}

	void AllowDash()
	{
		canDash = true;
        doctorRenderer.material = rechargeMaterial;
        AudioControl.Instance.PlayDashCharge();
        Invoke("ResetMaterial", 0.5f);
    }

	void CreateDustPlooms()
	{
		Invoke("CreateDustPloom", 0.1f);
		Invoke("CreateDustPloom", 0.3f);
		Invoke("CreateDustPloom", 0.6f);
	}

	void CreateDustPloom()
	{
		GameObject go = (GameObject)Instantiate(dustParticlePrefab, (transform.position-transform.forward), Quaternion.identity);
		Vector3 newPos = go.transform.position;
		newPos = new Vector3(newPos.x, 1.0f, newPos.z);
		//set direction of particles to point away from doctor
		go.transform.position = newPos;
		Vector3 direction = go.transform.position - transform.position;
		go.transform.rotation = Quaternion.LookRotation(direction);
	}


	public void ResetDash()
	{
		justDashed = false;
	}

    private void ResetMaterial() {
        doctorRenderer.material = normalMaterial;
    }

    public void informSurgeryFinished() {
		currentTool.gameObject.SetActive(true);
        inSurgery = false;
    }

    private void OnTutorialSurgeryStepComplete() {
        if (surgeryInput) {
            surgeryInput.ReturnControlToDoctor();
        }
        tutorialSurgeryStepComplete = true;
    }

    private void OnDestroy() {
        TutorialEventController.Instance.OnSurgeryOnPatientEnd -= OnTutorialSurgeryStepComplete;
    }

}
