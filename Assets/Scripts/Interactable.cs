﻿using UnityEngine;
using System.Collections;

public abstract class Interactable : MonoBehaviour {

    // called by the doctor to initate interaction with an interactive
    // (1) could have callback called in interactingDoctor when event is called if this is an important detail
    // (2) have to check tool type to make sure it is compatable and also could call a function in currentTool to let it know the interaction that is happening
    // returns true if can interact
    public virtual bool DocterIniatesInteracting(Doctor interactingDoctor) {
        interactingDoctor.currentTool.OnDoctorInitatedInteracting();
        return true;
    }

    public virtual void DoctorTerminatesInteracting(Doctor interactingDoctor) {
        interactingDoctor.currentTool.OnDoctorTerminatedInteracting();
    }

    // internal method for erturning the type that this interactable object requires
    protected abstract Tool.ToolType RequiredToolType();

}


