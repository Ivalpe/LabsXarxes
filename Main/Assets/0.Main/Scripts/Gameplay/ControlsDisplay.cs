using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ControlsDisplay : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject holdUI;
    
    void Start()
    {
        holdUI.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowHoldUI(string msg)
    {
        holdUI.SetActive(true);
        holdUI.GetComponentInChildren<TextMeshProUGUI>().text = msg;
    }

    public void HideHoldUI()
    {
        holdUI.SetActive(false);
    }
}
