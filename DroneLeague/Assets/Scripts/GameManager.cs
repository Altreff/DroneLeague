using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Teams Targets (Goals)")]
    public Transform goalTriggerA;
    public Transform goalTriggerB;


    [Header("Match Settings")]
    public int goalsToWinRound = 3; 
    public float roundDuration = 60f;
    public int totalRounds = 2;
    [Tooltip("Delay after goal")]
    public float delayAfterGoal = 3f;

    [Header("Teams")]
    public int teamAScore = 0;
    public int teamBScore = 0;

    [Header("Spawn Points")]
    public Transform[] teamASpawns; 
    public Transform[] teamBSpawns; 

    [Header("Drones")]
    public GameObject[] teamADrones; 
    public GameObject[] teamBDrones;

    [Header("Drones (Prefabs")]
    public GameObject dronePrefabTeamA;
    public GameObject dronePrefabTeamB;


    [Header("UI References")]
    public GameObject pauseMenu;
    public GameObject winnerScreen;
    public TextMeshProUGUI winnerText;

    [Header("Camera Reference")]
    public CameraController mainCamera;

    //Match Status
    private static int winsTeamA = 0;
    private static int winsTeamB = 0;
    private int currentRound = 0;
    private bool isPaused = false;

    //Current Round Timer
    private float roundTimer;
    private int scoreRoundA;
    private int scoreRoundB;
    private bool isRoundActive = false;


    private List<DroneInstance> activeDrones = new List<DroneInstance>();

    private class DroneInstance
    {
        public GameObject droneObject;
        public Transform spawnPoint;
        public Rigidbody rb;
    }

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
        Time.timeScale = 1f;
        StartMatch();
       
    }

    void Update()
    {

        if (Input.GetKeyDown(KeyCode.Escape) && !winnerScreen.activeSelf)
        {
            TogglePause();
        }

        if (isRoundActive && !isPaused)
        {
            roundTimer -= Time.deltaTime;

            if (roundTimer <= 0)
            {
                EndRound("TimeOut"); 
            }

        }
    }

    public void StartMatch()
    {

        currentRound = 0;
        Debug.Log($"Match Started! Global Score: A[{winsTeamA}] - B[{winsTeamB}]");
        StartNextRound();

        if (pauseMenu != null) pauseMenu.SetActive(false);
        if (winnerScreen != null) winnerScreen.SetActive(false);
    }

    public void StartNextRound()
    {
        currentRound++;

        if (currentRound > totalRounds)
        {
            EndMatch();
            return;
        }

        scoreRoundA = 0;
        scoreRoundB = 0;
        roundTimer = roundDuration;

        Debug.Log($"Round {currentRound} started!");
        SpawnOrResetDrones();

        StartCoroutine(GoalResetRoutine(false));
    }

    void SpawnOrResetDrones()
    {
        
        foreach (var d in activeDrones)
        {
            if (d.droneObject != null) Destroy(d.droneObject);
        }
        activeDrones.Clear();

        if (mainCamera != null)
        {
            mainCamera.target = null;
        }

        SpawnTeam(teamASpawns, dronePrefabTeamA, "TeamA");
        SpawnTeam(teamBSpawns, dronePrefabTeamB, "TeamB");
    }

    void SpawnTeam(Transform[] spawns, GameObject prefab, string teamName)
    {
        for (int i = 0; i < spawns.Length; i++)
        {
            if (spawns[i] == null) continue;
            GameObject newDrone = Instantiate(prefab, spawns[i].position, spawns[i].rotation);

            DroneController droneCtrl = newDrone.GetComponent<DroneController>();
            if (droneCtrl != null && mainCamera != null)
            {
                droneCtrl.cameraController = mainCamera;

                bool isPlayer = (teamName == "TeamA" && i == 0);
                droneCtrl.isHuman = isPlayer;
                if (isPlayer)
                {
                    mainCamera.target = newDrone.transform;
                    Debug.Log("Player spawned and linked to Camera!");

                }
                if (i == 0)
                {
                    droneCtrl.role = DroneRole.Attacker;
                }
                else
                {
                    droneCtrl.role = DroneRole.Defender;
                }
            }


            DroneInstance info = new DroneInstance();
            info.droneObject = newDrone;
            info.spawnPoint = spawns[i];
            info.rb = newDrone.GetComponent<Rigidbody>();

            if (!newDrone.CompareTag("Player")) newDrone.tag = "Player";
            activeDrones.Add(info);
        }
    }


    public void ScoreGoal(string teamScored)
    {
        if (!isRoundActive) return;
        isRoundActive = false;

        if (teamScored == "TeamA")
        {
            scoreRoundA++;
            Debug.Log($"GOAL Team A! Round score: {scoreRoundA}-{scoreRoundB}");
        }
        else if(teamScored == "TeamB")
        {
            scoreRoundB++;
            Debug.Log($"GOAL Team B! Round score: {scoreRoundB}-{scoreRoundB}");

        }
        if(scoreRoundA >= goalsToWinRound || scoreRoundB >= goalsToWinRound)
        {
            EndRound("The Round is Over");
        }
        else
        {
            StartCoroutine(GoalResetRoutine(true));
        }
    }

    IEnumerator GoalResetRoutine(bool wasGoal)
    {
        isRoundActive = false;
        if (wasGoal) Debug.Log("Goal scored! Resetting positions in 3 seconds...");

        SoftResetPositions(true);
        Debug.Log("Wait for start...");

        yield return new WaitForSeconds(delayAfterGoal);

        SoftResetPositions(false);
        isRoundActive = true;
        Debug.Log("GO!");
    }

    void SoftResetPositions(bool freeze)
    {
        foreach (var item in activeDrones)
        {
            if (item.droneObject == null) continue;
            if(item.rb != null)
            {
                if (freeze)
                {
                    if (!item.rb.isKinematic)
                    {
                        item.rb.linearVelocity = Vector3.zero;
                        item.rb.angularVelocity = Vector3.zero;
                    }
                    item.rb.isKinematic = true;
                }
                else
                {
                    item.rb.isKinematic = false;

                    item.rb.linearVelocity = Vector3.zero;
                    item.rb.angularVelocity = Vector3.zero;
                }
            }
            item.droneObject.transform.position = item.spawnPoint.position;
            item.droneObject.transform.rotation = item.spawnPoint.rotation;
        }
        Physics.SyncTransforms();
        Debug.Log("Positions reset after goal.");
    }

    void EndRound(string reason)
    {
        isRoundActive = false;
        string winner = "Draw";
        //Debug.Log($"Round {currentRound} ended!");

        //RoundEndEvent?.Invoke(winningTeam);
        
        if (scoreRoundA >= scoreRoundB)
        {
            winsTeamA++;
            winner = "Team A";
        }
        else if(scoreRoundB > scoreRoundA)
        {
            winsTeamB++;
            winner = "Team B";
        }

        Debug.Log($"Round {currentRound} is over ({reason})! Winner round is: {winner}");
        Debug.Log($"Current score by round -> A: {winsTeamA} | B: {winsTeamB}");

        Invoke("StartNextRound", 3f);
    }

    void EndMatch()
    {
        Debug.Log("=== MATCH IS OVER ===");
        string finalResult = "";
        if(winsTeamA > winsTeamB)
        {
            finalResult = "TEAM A LEADS!";
        }
        else if(winsTeamB > winsTeamA)
        {
            finalResult = "TEAM B LEADS!";
        }
        else
        {
            finalResult = "DRAW!";
        }

        if(winnerText != null ) winnerText.text = finalResult;
        if (winnerScreen != null) winnerScreen.SetActive(true);
        Time.timeScale = 0f;
        isRoundActive = false;
    }
    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = Color.white;
        if (isRoundActive)
        {
            GUI.Label(new Rect(10, 10, 300, 30), $"Round: {currentRound}/{totalRounds} | Time: {Mathf.Ceil(roundTimer)}", style);
            GUI.Label(new Rect(10, 40, 300, 30), $"Round Score: A [{scoreRoundA}] - B [{scoreRoundB}]", style);
            GUI.Label(new Rect(10, 70, 300, 30), $"Total Wins: A [{winsTeamA}] - B [{winsTeamB}]", style);
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
        winsTeamA = 0;
        winsTeamB = 0;
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
       
        //UnityEditor.EditorApplication.isPlaying = false;

    }

    public string GetScoreText()
    {
        return $"{teamAScore} - {teamBScore}";
    }

    public string GetTimerText()
    {
        int minutes = Mathf.FloorToInt(roundTimer / 60f);
        int seconds = Mathf.FloorToInt(roundTimer % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    public string GetRoundText()
    {
        return $"Round {currentRound}";
    }

}