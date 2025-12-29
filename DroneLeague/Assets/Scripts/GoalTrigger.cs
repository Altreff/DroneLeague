using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    [Header("Goal Settings")]
    public string teamToAwardPoint = "TeamA";

    private float lastScoreTime = -5f;
    private float cooldown = 1.0f;

    void OnTriggerEnter(Collider other)
    {
        if (Time.time < lastScoreTime + cooldown) return;

        GameObject rootObj = other.transform.root.gameObject;

        if (rootObj.CompareTag("Player") || rootObj.CompareTag("DroneAI"))
        {
            GameManager.Instance.ScoreGoal(teamToAwardPoint);

            lastScoreTime = Time.time;
            Debug.Log($"GOAL! Scored for {teamToAwardPoint}. Ring disabled for 1 sec.");
        }
    }
}
