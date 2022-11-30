using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        //Exit Level
        //TODO Bring up popup menu for navigation
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitLevel();
        }

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            RestartLevel();
        }
    }

    public void ExitLevel()
    {
        //Load new level
        SceneManager.LoadScene("MainMenu");
    }

    public void RestartLevel()
    {
        int activeSceneIndex =
        SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(activeSceneIndex);
    }
}
