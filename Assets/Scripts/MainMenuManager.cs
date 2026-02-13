using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "SampleScene";

    public void OnStartRun()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnWeapons()
    {
        Debug.Log("Weapons menu (placeholder)");
        // panel açacağız
    }

    public void OnItems()
    {
        Debug.Log("Items menu (placeholder)");
    }

    public void OnComingSoon()
    {
        Debug.Log("Coming Soon!");
        // küçük popup
    }
}
