using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIInteraction : MonoBehaviour
{
    public void OnObjectSelected(string filename, string name, string category)
    {
        SelectedObjectData.objectFilename = filename;
        SelectedObjectData.objectName = name;
        SelectedObjectData.objectCategory = category;

        Debug.Log("Object selected: " + name + " " +filename);
        SceneManager.LoadScene("ARPlacementSCene", LoadSceneMode.Single);
    }
}