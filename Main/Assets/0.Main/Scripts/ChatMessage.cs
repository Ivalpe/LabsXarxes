using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class ChatMessage : MonoBehaviour
{
    [SerializeField] public TextMeshProUGUI messageText;
    [SerializeField] public RawImage pfpImage;

    [System.Serializable]
    public struct ColorToImage
    {
        public string colorName;
        public byte index;
        public Texture2D avatarTex;
    }

    [SerializeField]
    public List<ColorToImage> colorAvatars = new List<ColorToImage>
    {
        new ColorToImage{colorName = "Red", index = 0},
        new ColorToImage{colorName = "Blue", index = 1},
        new ColorToImage{colorName = "Green", index = 2},
        new ColorToImage{colorName = "Orange", index = 3},
        new ColorToImage{colorName = "Magenta", index = 4},
        new ColorToImage{colorName = "Cyan", index = 5}
    };

    public void SetText(string text, byte color)
    {
        messageText.text = text;
        Texture2D avatar = colorAvatars[color].avatarTex;
        pfpImage.texture = avatar;
    }
}
