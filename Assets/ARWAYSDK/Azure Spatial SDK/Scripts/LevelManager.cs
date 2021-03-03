using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static string levelToLoad;
    public GameObject CrossfadeAnim;

    private float _progress = 0f;

    private void Start()
    {
        if (CrossfadeAnim == null)
        {
            CrossfadeAnim = new GameObject("Crossfade Anim");
        }
    }

    public void LoadLevel(string levelName)
    {
        levelToLoad = levelName;
        Debug.Log("New Level Load : " + levelToLoad);

        CrossfadeAnim.SetActive(true);
        StartCoroutine(LoadYourAsyncScene());
    }

    IEnumerator LoadYourAsyncScene()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(levelToLoad);
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9F)
        {
            yield return null;
        }

        Debug.Log(asyncLoad.progress);
        yield return new WaitForSeconds(0.8f);

        // while (!_isClicked) // While condition == false
        // {
        //     yield return null;
        // }

        asyncLoad.allowSceneActivation = true;
    }

    public void QuitRequest()
    {
        Debug.Log("Quit Requested!");
        Application.Quit();
    }
}