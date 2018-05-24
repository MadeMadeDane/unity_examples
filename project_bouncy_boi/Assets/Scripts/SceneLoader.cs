
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour {

    public void OnClick()
    {
        //LoadScene(scene);
    }

    public void LoadScene(String scene)
    {
        Debug.Log("Loading scene: " + scene);
        SceneManager.LoadScene(scene);
    }
}