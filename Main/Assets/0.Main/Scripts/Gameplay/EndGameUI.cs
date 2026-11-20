using UnityEngine;
using System.IO;

public class EndGameUI : NetObject
{
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;

    private ConnectionManager serverManager;

    private void Start()
    {
        if (serverManager == null) serverManager = FindAnyObjectByType<ConnectionManager>();

        // Al cargarse la escena, busca el ClientNetworkManager persistente y le entrega las referencias
        if (ClientNetworkManager.Instance != null)
        {
            ClientNetworkManager.Instance.RegisterEndGamePanels(winPanel, losePanel);
        }

        // Aseguramos que empiecen apagados
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }

    void Update()
    {
        if(serverManager != null)
        {
            if (serverManager.gameOver == 1 && winPanel != null)
            {
                winPanel.SetActive(true);
            }
            else if (serverManager.gameOver == 2 && losePanel != null)
            {
                losePanel.SetActive(true);

            }
        }
        
    }


    public override void Write(BinaryWriter w)
    {
        if (serverManager == null) serverManager = FindAnyObjectByType<ConnectionManager>();
        if (serverManager != null)
        {
            w.Write(serverManager.gameOver);
        }
    }


    public override void Read(BinaryReader r)
    {

        byte gameOverState = r.ReadByte();

        if (ClientNetworkManager.Instance != null)
        {
            if (gameOverState == 1)
            {
                ClientNetworkManager.Instance.ShowWinScreen();
            }
            else if (gameOverState == 2)
            {
                ClientNetworkManager.Instance.ShowLoseScreen();
            }
        }
    }
}