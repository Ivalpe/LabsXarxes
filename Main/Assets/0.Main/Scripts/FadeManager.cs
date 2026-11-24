using System.Collections;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FadeManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private Image fadePanel;
    GameManager gameManager;
    void Start()
    {
        if (fadePanel) fadePanel.gameObject.SetActive(false);
        gameManager = FindAnyObjectByType<GameManager>();

        if (!gameManager) // will only trigger if went back to menu from gamescene (Back To Main Menu Button)
        {
            gameManager = new GameManager();
        }
    }

    // Update is called once per frame
    void Update()
    {

    }


    public IEnumerator Fade(float startAlpha, float endAlpha, float duration, float delay, bool changeScene, string sceneName = "")
    {
        fadePanel.gameObject.SetActive(true);

        yield return new WaitForSeconds(delay);

        float timeElapsed = 0;
        while (timeElapsed < duration)
        {
            float fadePercentage = Mathf.Clamp01(timeElapsed / duration);

            fadePanel.color = new Color(fadePanel.color.r, fadePanel.color.g, fadePanel.color.b, endAlpha * fadePercentage);
            timeElapsed += Time.deltaTime;
            yield return null;
        }


        if (endAlpha == 0.0)
        {
            fadePanel.gameObject.SetActive(false);
        }
        else if (endAlpha == 1.0f)
        {
            fadePanel.gameObject.SetActive(true);
        }

        if (changeScene)
        {
            if (SceneManager.GetSceneByName(sceneName) != null)
            {
                GameManager gm = FindAnyObjectByType<GameManager>();
                Destroy(gm.gameObject);
                gm = null;

                ConnectionManager serverManager = ConnectionManager.Instance;

                if (serverManager) 
                {
                    serverManager.ctpc.Disconnect();
                    serverManager.cudp.Disconnect();
                    Destroy(serverManager.gameObject);
                }

                ClientNetworkManager clientManager = ClientNetworkManager.Instance;

                if (clientManager)
                {
                    clientManager.TCPconnection.Disconnect();
                    clientManager.UDPconnection.Disconnect();
                    Destroy(clientManager.gameObject);

                    
                }
                SceneManager.LoadScene(sceneName);
            }
        }
    }
}
