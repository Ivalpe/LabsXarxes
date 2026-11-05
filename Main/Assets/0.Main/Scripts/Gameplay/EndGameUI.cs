using UnityEngine;

public class EndGameUI : MonoBehaviour
{
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;

    private void Start()
    {
        // Al cargarse la escena, busca el ClientNetworkManager persistente y le entrega las referencias
        if (ClientNetworkManager.Instance != null)
        {
            ClientNetworkManager.Instance.RegisterEndGamePanels(winPanel, losePanel);
        }

        // Aseguramos que empiecen apagados
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }
}