using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("HUD Elements")]
    [SerializeField] private Text waveText;
    [SerializeField] private Text healthText;
    [SerializeField] private Text enemiesRemainingText;
    [SerializeField] private GameObject hudPanel;
    
    [Header("Pause Panel")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button endRunButton;
    
    [Header("Game Over Panel")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Text gameOverMessageText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;
    
    [Header("References")]
    [SerializeField] private PlayerBehavior player;
    
    private bool isPaused = false;
    private int survivedWaves = 0;
    
    void Start()
    {
        // Subscribe to events
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveChanged += UpdateWaveDisplay;
            WaveManager.Instance.OnWaveCleared += OnWaveCleared;
        }
        
        if (player != null)
        {
            player.OnPlayerDeath += ShowGameOverPanel;
            player.OnHealthChanged += UpdateHealthDisplay;
        }
        
        // Setup button listeners
        if (continueButton != null)
            continueButton.onClick.AddListener(ContinueGame);
        
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);
        
        if (endRunButton != null)
            endRunButton.onClick.AddListener(EndRun);
        
        if (retryButton != null)
            retryButton.onClick.AddListener(RestartGame);
        
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(LoadMainMenu);
        
        // Initialize UI
        HideAllPanels();
        hudPanel.SetActive(true);
        pausePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        
        UpdateWaveDisplay(1);
        // if (player != null)
        //     UpdateHealthDisplay(player.GetCurrentHealth());
    }
    
    void Update()
    {
        // Pause game with Escape key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (gameOverPanel.activeSelf) return;
            
            if (isPaused)
            {
                ContinueGame();
            }
            else
            {
                PauseGame();
            }
        }
        
        // Update enemies remaining text
        if (WaveManager.Instance != null && enemiesRemainingText != null)
        {
            int remaining = WaveManager.Instance.GetRemainingEnemies();
            int total = WaveManager.Instance.GetTotalEnemiesInWave();
            enemiesRemainingText.text = $"Enemies: {remaining}/{total}";
        }
    }
    
    void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        hudPanel.SetActive(false);
        pausePanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    void ContinueGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        hudPanel.SetActive(true);
        pausePanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    void EndRun()
    {
        // Automatically kill the player
        if (player != null)
        {
            player.Die();
        }
        else
        {
            ShowGameOverPanel(0);
        }
    }
    
    void ShowGameOverPanel(int wavesSurvived)
    {
        survivedWaves = wavesSurvived;
        Time.timeScale = 0f;
        hudPanel.SetActive(false);
        pausePanel.SetActive(false);
        gameOverPanel.SetActive(true);
        
        if (gameOverMessageText != null)
        {
            gameOverMessageText.text = $"Congrats! You survived {survivedWaves} waves!";
        }
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
    
    void UpdateWaveDisplay(int waveNumber)
    {
        if (waveText != null)
        {
            waveText.text = $"Wave: {waveNumber}";
        }
    }
    
    void UpdateHealthDisplay(int health)
    {
        if (healthText != null)
        {
            healthText.text = $"Health: {health}";
        }
    }
    
    void OnWaveCleared(int waveNumber)
    {
        Debug.Log($"Wave {waveNumber} cleared!");
        // Optional: Add wave cleared effects or sounds here
    }
    
    void HideAllPanels()
    {
        if (hudPanel != null) hudPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveChanged -= UpdateWaveDisplay;
            WaveManager.Instance.OnWaveCleared -= OnWaveCleared;
        }
        
        if (player != null)
        {
            player.OnPlayerDeath -= ShowGameOverPanel;
            player.OnHealthChanged -= UpdateHealthDisplay;
        }
    }
}