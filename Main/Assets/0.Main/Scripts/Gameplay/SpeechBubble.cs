using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class SpeechBubble : NetObject
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField] public Sprite[] catMemes;
    [SerializeField] Image catIMG;

    bool set = false;

    public int memeIndex = 0;

    void Start()
    {
        
    }

    public void SetCatMeme(int memeIndex)
    {
        if (catIMG && catMemes.Length > 0 && !set)
        {
            catIMG.sprite = catMemes[memeIndex];
            set = true;
        }
    }

    public override void Read(BinaryReader r)
    {

        memeIndex = r.ReadInt32();
        SetCatMeme(memeIndex);
    }
    
    public override void Write(BinaryWriter w)
    {
        w.Write(memeIndex);
        SetCatMeme(memeIndex);
    }
}
