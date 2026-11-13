using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class ChatMessage : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI messageText;

    public void SetText(string text)
    {
        messageText.text = text;
    }
}
