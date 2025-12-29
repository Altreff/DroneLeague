using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Teams Targets (Goals)")]
    public Transform teamAAttackGate;
    public Transform teamADefendGate;
    public Transform teamBAttackGate;
    public Transform teamBDefendGate;

    [Header("Match Settings")]
    public int goalsToWinRound = 3;
    public float roundDuration = 60f;
    public int totalRounds = 2;
    public float delayAfterGoal = 3f;

    [Header("Teams")]
    public int teamAScore = 0;
    public int teamBScore = 0;

    [Header("Spawn Points")]
    public Transform[] teamASpawns;
    public Transform[] teamBSpawns;

    [Header("Drones Prefabs")]
    public GameObject playerPrefab;
    public GameObject strikerPrefab;
    public GameObject defenderPrefab;

    [Header("Team Materials")]
    public Material teamAMaterial; // красный
    public Material teamBMaterial; // синий

    [Header("UI References")]
    public GameObject pauseMenu;
    public GameObject winnerScreen;
    public GameObject teamSelectionMenu;
    public TextMeshProUGUI winnerText;
    public TextMeshProUGUI globalScoreText;

    [Header("Camera Reference")]
    public CameraController mainCamera;

    private static int globalMatchesWonA = 0;
    private static int globalMatchesWonB = 0;
    private int currentMatchRoundsA = 0;
    private int currentMatchRoundsB = 0;
    private int currentRound = 0;
    private bool isPaused = false;

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
        public string teamName;
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        Time.timeScale = 1f;

        if (teamSelectionMenu != null)
        {
            teamSelectionMenu.SetActive(true);
            if (pauseMenu != null) pauseMenu.SetActive(false);
            if (winnerScreen != null) winnerScreen.SetActive(false);
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
        if (teamSelectionMenu != null) teamSelectionMenu.SetActive(false);
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
            if (roundTimer <= 0) EndRound("TimeOut");
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

        SpawnTeam(teamASpawns, "TeamA", teamAAttackGate, teamADefendGate, teamAMaterial);
        SpawnTeam(teamBSpawns, "TeamB", teamBAttackGate, teamBDefendGate, teamBMaterial);

        AssignThreatTargets();
    }

    void SpawnTeam(Transform[] spawns, string teamName, Transform attackGate, Transform defendGate, Material teamMaterial)
    {
        for (int i = 0; i < spawns.Length; i++)
        {
            if (spawns[i] == null) continue;

            GameObject prefabToUse = defenderPrefab;
            bool isPlayer = (teamName == playerChosenTeam && i == 0);

            if (isPlayer) prefabToUse = playerPrefab;
            else if (i == 0) prefabToUse = strikerPrefab;

            if (prefabToUse == null) prefabToUse = defenderPrefab;

            GameObject newDroneRoot = Instantiate(prefabToUse, spawns[i].position, spawns[i].rotation);
            Rigidbody rb = newDroneRoot.GetComponentInChildren<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            DroneController droneCtrl = newDroneRoot.GetComponentInChildren<DroneController>();

            // Игрок
            if (isPlayer && droneCtrl != null)
            {
                droneCtrl.controlMode = DroneControlMode.Player;
                if (mainCamera != null)
                {
                    mainCamera.target = droneCtrl.transform;
                    droneCtrl.cameraController = mainCamera;
                }
            }
            else if (droneCtrl != null)
            {
                // AI-дрон
                droneCtrl.controlMode = DroneControlMode.AI;
                if (i == 0)
                {
                    var strikerAI = newDroneRoot.GetComponentInChildren<DroneStrikerAI>();
                    if (strikerAI != null && attackGate != null)
                    {
                        GateTarget gateComp = attackGate.GetComponent<GateTarget>();
                        if (gateComp != null) strikerAI.targetGate = gateComp;
                    }
                }
                else
                {
                    var defenderAI = newDroneRoot.GetComponentInChildren<DroneDefenderAI>();
                    if (defenderAI != null && defendGate != null)
                    {
                        GateTarget gateComp = defendGate.GetComponent<GateTarget>();
                        if (gateComp != null) defenderAI.defendGate = gateComp;
                    }
                }
            }

            ApplyTeamMaterialToDrone(newDroneRoot, teamMaterial);

            DroneInstance info = new DroneInstance();
            info.droneObject = newDroneRoot;
            info.spawnPoint = spawns[i];
            info.rb = rb;
            info.teamName = teamName;
            activeDrones.Add(info);
        }
    }

    private void ApplyTeamMaterialToDrone(GameObject droneRoot, Material teamMaterial)
    {
        if (droneRoot == null || teamMaterial == null) return;

        Transform sphere = FindChildRecursive(droneRoot.transform, "Sphere");
        if (sphere != null)
        {
            Renderer rend = sphere.GetComponent<Renderer>();
            if (rend != null) rend.material = teamMaterial;
        }
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private void AssignThreatTargets()
    {
        Transform teamBAttacker = null;
        Transform teamAAttacker = null;

        foreach (var d in activeDrones)
        {
            DroneStrikerAI striker = d.droneObject.GetComponentInChildren<DroneStrikerAI>();
            DroneController player = d.droneObject.GetComponentInChildren<DroneController>();

            if (d.teamName == "TeamB" && (striker != null || player != null) && teamBAttacker == null)
                teamBAttacker = d.droneObject.transform;
            if (d.teamName == "TeamA" && (striker != null || player != null) && teamAAttacker == null)
                teamAAttacker = d.droneObject.transform;
        }

        foreach (var d in activeDrones)
        {
            DroneDefenderAI defender = d.droneObject.GetComponentInChildren<DroneDefenderAI>();
            if (defender != null)
            {
                if (d.teamName == "TeamA") defender.threatTarget = teamBAttacker;
                else defender.threatTarget = teamAAttacker;
            }
        }
    }

    public void ScoreGoal(string teamScored)
    {
        if (!isRoundActive) return;
        isRoundActive = false;

        if (teamScored == "TeamA") scoreRoundA++;
        else if (teamScored == "TeamB") scoreRoundB++;

        if (scoreRoundA >= goalsToWinRound || scoreRoundB >= goalsToWinRound)
            EndRound("The Round is Over");
        else
            StartCoroutine(GoalResetRoutine(true));
    }

    IEnumerator GoalResetRoutine(bool wasGoal)
    {
        isRoundActive = false;
        if (wasGoal) { FreezeAllDrones(); yield return new WaitForSeconds(delayAfterGoal); SpawnOrResetDrones(); }
        else yield return new WaitForSeconds(delayAfterGoal);

        UnfreezeAllDrones();
        isRoundActive = true;
    }

    void UnfreezeAllDrones()
    {
        foreach (var item in activeDrones)
        {
            if (item.rb != null) item.rb.isKinematic = false;
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

    void EndRound(string reason)
    {
        isRoundActive = false;
        string winner = "Draw";

        if (scoreRoundA >= scoreRoundB) { currentMatchRoundsA++; winner = "Team A"; }
        else if (scoreRoundB > scoreRoundA) { currentMatchRoundsB++; winner = "Team B"; }

        Debug.Log($"Round {currentRound} is over ({reason})! Winner round: {winner}");
        Invoke("StartNextRound", 3f);
    }

    void EndMatch()
    {
        string finalResult = "";
        if (currentMatchRoundsA > currentMatchRoundsB) { finalResult = "TEAM A WINS THE MATCH!"; globalMatchesWonA++; }
        else if (currentMatchRoundsB > currentMatchRoundsA) { finalResult = "TEAM B WINS THE MATCH"; globalMatchesWonB++; }
        else finalResult = "DRAW!";

        if (winnerText != null) winnerText.text = finalResult;
        if (globalScoreText != null)
            globalScoreText.text = $"Session History: A[{globalMatchesWonA}] - B[{globalMatchesWonB}]";

        if (winnerScreen != null) winnerScreen.SetActive(true);
        Time.timeScale = 0f;
        isRoundActive = false;
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        if (pauseMenu != null) pauseMenu.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;
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
        Application.Quit();
    }

    public string GetScoreText() => $"{teamAScore} - {teamBScore}";
    public string GetTimerText() { int minutes = Mathf.FloorToInt(roundTimer / 60f); int seconds = Mathf.FloorToInt(roundTimer % 60f); return $"{minutes:00}:{seconds:00}"; }
    public string GetRoundText() => $"Round {currentRound}";
}
