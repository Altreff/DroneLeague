using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Match Settings")]
    public int roundsToWin = 2; 
    public float roundDuration = 120f; 

    [Header("Teams")]
    public int teamAScore = 0;
    public int teamBScore = 0;

    [Header("Current Round")]
    public int currentRound = 1;
    public float roundTimeRemaining;
    public bool isRoundActive = false;
    public bool isMatchOver = false;

    [Header("Spawn Points")]
    public Transform[] teamASpawnPoints; 
    public Transform[] teamBSpawnPoints; 

    [Header("Drones")]
    public GameObject[] teamADrones; 
    public GameObject[] teamBDrones; 

    [Header("UI References")]
    public GameObject pauseMenu;
    public GameObject winnerScreen;

    public delegate void OnGoalScored(string teamName);
    public static event OnGoalScored GoalScoredEvent;

    public delegate void OnRoundEnd(string winningTeam);
    public static event OnRoundEnd RoundEndEvent;

    public delegate void OnMatchEnd(string winningTeam);
    public static event OnMatchEnd MatchEndEvent;

    private bool isPaused = false;

    void Awake()
    {
     
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        InitializeMatch();
        StartRound();
    }

    void Update()
    {
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        if (isRoundActive && !isPaused)
        {
            roundTimeRemaining -= Time.deltaTime;

            if (roundTimeRemaining <= 0)
            {
                roundTimeRemaining = 0;
                EndRound(null); 
            }
        }
    }

    void InitializeMatch()
    {
        teamAScore = 0;
        teamBScore = 0;
        currentRound = 1;
        isMatchOver = false;

        if (pauseMenu != null) pauseMenu.SetActive(false);
        if (winnerScreen != null) winnerScreen.SetActive(false);
    }

    public void StartRound()
    {
        roundTimeRemaining = roundDuration;
        isRoundActive = true;

        
        ResetDronesToSpawn();

        Debug.Log($"Round {currentRound} started!");
    }

    void ResetDronesToSpawn()
    {
        for (int i = 0; i < teamADrones.Length && i < teamASpawnPoints.Length; i++)
        {
            if (teamADrones[i] != null && teamASpawnPoints[i] != null)
            {
                teamADrones[i].transform.position = teamASpawnPoints[i].position;
                teamADrones[i].transform.rotation = teamASpawnPoints[i].rotation;

                
                Rigidbody rb = teamADrones[i].GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }

        for (int i = 0; i < teamBDrones.Length && i < teamBSpawnPoints.Length; i++)
        {
            if (teamBDrones[i] != null && teamBSpawnPoints[i] != null)
            {
                teamBDrones[i].transform.position = teamBSpawnPoints[i].position;
                teamBDrones[i].transform.rotation = teamBSpawnPoints[i].rotation;

                Rigidbody rb = teamBDrones[i].GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }
    }

    public void RegisterGoal(string teamName)
    {
        if (!isRoundActive || isMatchOver) return;

        if (teamName == "TeamA")
        {
            teamAScore++;
            Debug.Log($"Team A scored! Score: {teamAScore} - {teamBScore}");
        }
        else if (teamName == "TeamB")
        {
            teamBScore++;
            Debug.Log($"Team B scored! Score: {teamAScore} - {teamBScore}");
        }

        
        GoalScoredEvent?.Invoke(teamName);

        
        if (teamAScore >= roundsToWin)
        {
            EndMatch("TeamA");
        }
        else if (teamBScore >= roundsToWin)
        {
            EndMatch("TeamB");
        }
        else
        {
            
            Invoke(nameof(ResetAfterGoal), 2f); 
        }
    }

    void ResetAfterGoal()
    {
        ResetDronesToSpawn();
    }

    void EndRound(string winningTeam)
    {
        isRoundActive = false;

        Debug.Log($"Round {currentRound} ended! Winner: {(winningTeam != null ? winningTeam : "Draw")}");

        RoundEndEvent?.Invoke(winningTeam);

        if (!isMatchOver)
        {
            currentRound++;
            Invoke(nameof(StartRound), 3f); 
        }
    }

    void EndMatch(string winningTeam)
    {
        isMatchOver = true;
        isRoundActive = false;

        Debug.Log($"Match Over! {winningTeam} wins!");

        MatchEndEvent?.Invoke(winningTeam);

        if (winnerScreen != null)
        {
            winnerScreen.SetActive(true);
            
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (pauseMenu != null)
        {
            pauseMenu.SetActive(isPaused);
        }

        Time.timeScale = isPaused ? 0f : 1f;

        Debug.Log(isPaused ? "Game Paused" : "Game Resumed");
    }

    public void ResumeGame()
    {
        isPaused = false;
        if (pauseMenu != null) pauseMenu.SetActive(false);
        Time.timeScale = 1f;
    }

    public void RestartMatch()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ExitToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }

    public string GetScoreText()
    {
        return $"{teamAScore} - {teamBScore}";
    }

    public string GetTimerText()
    {
        int minutes = Mathf.FloorToInt(roundTimeRemaining / 60f);
        int seconds = Mathf.FloorToInt(roundTimeRemaining % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    public string GetRoundText()
    {
        return $"Round {currentRound}";
    }
}