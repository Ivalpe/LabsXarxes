using UnityEngine;
using UnityEngine.UI;

public class PlayerIconManager : MonoBehaviour
{
    Image playerIcon;
    void Start()
    {
        //playerIcon = this.gameObject.GetComponent<Image>(); 
    }

    // Update is called once per frame

    public void SetPlayerIconColor(Color _color)
    {
        this.gameObject.GetComponent<Image>().color = _color;
    }

}
