using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    [Header("Goal Settings")]
    public string teamToScore = "TeamA";

    /*   
    void Awake()
    {
        // Убедитесь, что у ворот есть триггер
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

    }
    */

    void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("Player"))
        {
            Debug.Log($"GOAL! {teamToScore} scores");

            GameManager.Instance.ScoreGoal(teamToScore);
        }
    }
}