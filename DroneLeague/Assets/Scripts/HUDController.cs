using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour
{
    [Header("Text Elements")]
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI roundScoreText;
    public TextMeshProUGUI matchScoreText;
    public TextMeshProUGUI globalWinsText;


    GameManager gm;

    void Start()
    {
        gm = GameManager.Instance;
    }

    void Update()
    {
        if (!gm || !gm.isRoundActive)
            return;

        roundText.text = $"Round {gm.currentRound}/{gm.totalRounds}";
        timerText.text = gm.GetTimerText();
        scoreText.text = gm.GetScoreText();

        roundScoreText.text =
            $"Round Score: A [{gm.scoreRoundA}] - B [{gm.scoreRoundB}]";

        matchScoreText.text =
            $"Match Rounds: A [{gm.currentMatchRoundsA}] - B [{gm.currentMatchRoundsB}]";

        globalWinsText.text =
            $"Total Wins: A [{GameManager.globalMatchesWonA}] - B [{GameManager.globalMatchesWonB}]";
    }
}
