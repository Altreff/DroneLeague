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
    public GameObject dronePrefabTeamA;
    public GameObject dronePrefabTeamB;


    [Header("Drones (Prefabs")]
    public GameObject playerPrefab;
    public GameObject strikerPrefab;
    public GameObject defenderPrefab;


    [Header("UI References")]
    public GameObject pauseMenu;
    public GameObject winnerScreen;
    public GameObject teamSelectionMenu;
    public TextMeshProUGUI winnerText;
    public TextMeshProUGUI globalScoreText;    

    [Header("Camera Reference")]
    public CameraController mainCamera;

    //Match Status
    private static int globalMatchesWonA = 0;
    private static int globalMatchesWonB = 0;
    private int currentMatchRoundsA = 0;
    private int currentMatchRoundsB = 0;
    private int currentRound = 0;
    private bool isPaused = false;

    //Current Round Timer
    private float roundTimer;
    private int scoreRoundA;
    private int scoreRoundB;
    private bool isRoundActive = false;

    private string playerChosenTeam = "";

    private List<DroneInstance> activeDrones = new List<DroneInstance>();

    private class DroneInstance
    {
        public GameObject droneObject;
        public Transform spawnPoint;
        public Rigidbody rb;
    }

    private GateTarget gateTargetA;
    private GateTarget gateTargetB;

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

        if(goalTriggerA != null)
        {
            gateTargetA = goalTriggerA.GetComponent<GateTarget>();
            if(gateTargetA == null) gateTargetA = gateTargetA.gameObject.AddComponent<GateTarget>();
        }

        if (goalTriggerB != null)
        {
            gateTargetB = goalTriggerB.GetComponent<GateTarget>();
            if( gateTargetB == null) gateTargetB =gateTargetB.gameObject.AddComponent<GateTarget>();
        }

        if (teamSelectionMenu != null)
        {
            teamSelectionMenu.SetActive(true);
            if(pauseMenu != null) pauseMenu.SetActive(false);
            if(winnerScreen !=null) winnerScreen.SetActive(false);
        }
        else
        {
            ChooseTeam("TeamA");
        }
    }
    public void ChooseTeam(string teamName)
    {
        playerChosenTeam = teamName;
        Debug.Log($"Player chose: {playerChosenTeam}");
        if(teamSelectionMenu!=null) teamSelectionMenu.SetActive(false);
        StartMatch();

    }


    void Update()
    {

        if (Input.GetKeyDown(KeyCode.Escape) && !winnerScreen.activeSelf && (teamSelectionMenu == null || !teamSelectionMenu.activeSelf))
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
        Debug.Log($"Match Started! Global Score: A[{globalMatchesWonA}] - B[{globalMatchesWonB}]");
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

        SpawnTeam(teamASpawns, "TeamA", gateTargetB, gateTargetA);
        SpawnTeam(teamBSpawns, "TeamB", gateTargetA, gateTargetB);
    }

    void SpawnTeam(Transform[] spawns, string teamName, GateTarget attackGate, GateTarget defendGate)
    {
        for (int i = 0; i < spawns.Length; i++)
        {
            if (spawns[i] == null) continue;

            // 1. Выбор префаба
            GameObject prefabToUse = defenderPrefab;
            bool isPlayer = (teamName == playerChosenTeam && i == 0);

            if (isPlayer)
                prefabToUse = playerPrefab;
            else if (i == 0)
                prefabToUse = strikerPrefab;

            if (prefabToUse == null)
                prefabToUse = (teamName == "TeamA") ? dronePrefabTeamA : dronePrefabTeamB;

            // 2. Создаем объект (это "Коробка" дрона)
            GameObject newDroneRoot = Instantiate(prefabToUse, spawns[i].position, spawns[i].rotation);

            // --- ИСПРАВЛЕНИЕ 1: Ищем компоненты ВНУТРИ коробки (InChildren) ---
            Rigidbody rb = newDroneRoot.GetComponentInChildren<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true; // Теперь заморозка сработает!
            }

            DroneController droneCtrl = newDroneRoot.GetComponentInChildren<DroneController>();

            if (droneCtrl != null)
            {
                if (isPlayer)
                {
                    droneCtrl.controlMode = DroneControlMode.Player;
                    if (mainCamera != null)
                    {
                        // --- ИСПРАВЛЕНИЕ 2: Привязываем камеру к САМОМУ ДРОНУ, а не к коробке
                        mainCamera.target = droneCtrl.transform;
                        droneCtrl.cameraController = mainCamera;
                        Debug.Log($">>> SUCCESS: Camera linked to {droneCtrl.name}!");
                    }
                }
                else
                {
                    droneCtrl.controlMode = DroneControlMode.AI;

                    var strikerAI = newDroneRoot.GetComponentInChildren<DroneStrikerAI>();
                    if (strikerAI != null) strikerAI.targetGate = attackGate;

                    var defenderAI = newDroneRoot.GetComponentInChildren<DroneDefenderAI>();
                    if (defenderAI != null) defenderAI.defendGate = defendGate;
                }
            }

            DroneInstance info = new DroneInstance();
            info.droneObject = newDroneRoot;
            info.spawnPoint = spawns[i];
            info.rb = rb;

            // Тег вешаем на саму физическую тушку
            if (droneCtrl != null && !droneCtrl.gameObject.CompareTag("Player"))
            {
                droneCtrl.gameObject.tag = "Player";
            }

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
        if (wasGoal)
        {
            FreezeAllDrones();
            yield return new WaitForSeconds(delayAfterGoal);

            Debug.Log("Respawning teams...");
            SpawnOrResetDrones();
        }
        else
        {
            Debug.Log("Wait for start...");
            yield return new WaitForSeconds(delayAfterGoal);
        }

        UnfreezeAllDrones();
        isRoundActive = true;
        Debug.Log("GO!");
    }

    void UnfreezeAllDrones()
    {
        foreach (var item in activeDrones)
        {
            if(item.rb != null)
            {
                item.rb.isKinematic = false;
            }
        }
    }
    void FreezeAllDrones()
    {
        foreach (var item in activeDrones)
        {
            if (item.droneObject == null) continue;
            if (item.rb != null)
            {
                item.rb.linearVelocity = Vector3.zero;
                item.rb.angularVelocity = Vector3.zero;
                item.rb.isKinematic = true;
            }
        }
    }
    /*
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
    */
    void EndRound(string reason)
    {
        isRoundActive = false;
        string winner = "Draw";
        
        if (scoreRoundA >= scoreRoundB)
        {
            currentMatchRoundsA++;
            winner = "Team A";
        }
        else if(scoreRoundB > scoreRoundA)
        {
            currentMatchRoundsB++;
            winner = "Team B";
        }

        Debug.Log($"Round {currentRound} is over ({reason})! Winner round is: {winner}");
        Debug.Log($"Current score by round -> A: {currentMatchRoundsA} | B: {currentMatchRoundsB}");

        Invoke("StartNextRound", 3f);
    }

    void EndMatch()
    {
        Debug.Log("=== MATCH IS OVER ===");
        string finalResult = "";
        if(currentMatchRoundsA > currentMatchRoundsB)
        {
            finalResult = "TEAM A WINS THE MATCH!";
            globalMatchesWonA++;
        }
        else if(currentMatchRoundsB > currentMatchRoundsA)
        {
            finalResult = "TEAM B WINS THE MATCH";
            globalMatchesWonB++;
        }
        else
        {
            finalResult = "DRAW!";
        }

        if(winnerText != null ) winnerText.text = finalResult;
        if(globalScoreText != null)
        {
            globalScoreText.text = $"Session History: A[{globalMatchesWonA}] - B[{globalMatchesWonB}]";
        }

        if (winnerScreen != null) winnerScreen.SetActive(true);
        Time.timeScale = 0f;
        isRoundActive = false;
    }
    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 14;
        style.normal.textColor = Color.white;
        if (isRoundActive)
        {
            GUI.Label(new Rect(10, 10, 300, 30), $"Round: {currentRound}/{totalRounds} | Time: {Mathf.Ceil(roundTimer)}", style);
            GUI.Label(new Rect(10, 40, 300, 30), $"Round Score: A [{scoreRoundA}] - B [{scoreRoundB}]", style);
            GUI.Label(new Rect(10, 70, 300, 30), $"Match Rounds: A [{currentMatchRoundsA}] - B [{currentMatchRoundsB}]", style);
            GUI.Label(new Rect(10, 100, 300, 30), $"Total Wins: A [{globalMatchesWonA}] - B [{globalMatchesWonB}]", style);
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
        globalMatchesWonA = 0;
        globalMatchesWonB = 0;
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