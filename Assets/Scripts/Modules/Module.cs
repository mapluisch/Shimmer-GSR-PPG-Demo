using UnityEngine;

public abstract class Module : MonoBehaviour
{
    public bool isModuleUsable; // whether or not a sensor is readable & the corresponding module is usable
    public abstract string GetDataString(); // should return the json-ified data string of a module
    public abstract string GetDetails(); // should return some additional detail string; SN if possible, otherwise hardware name (i.e. FLIR Thermal Camera);
    public abstract object GetDataFrame(); // should return the data frame (and content thereof) per module; best to create a fresh dataframe and fill it per module

    public void SetModuleUsable(bool state)
    {
        this.isModuleUsable = state;

        if (state) EventController.TriggerOnModuleUsable(this);
        else EventController.TriggerOnModuleUnusable(this);
    }
}