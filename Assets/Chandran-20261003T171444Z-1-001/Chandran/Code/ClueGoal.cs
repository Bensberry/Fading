using UnityEngine;

public class ClueGoal : MonoBehaviour
{
    [Header("Clue State")]
    public bool goalAchieved = false;

    [Header("Scoring")]
    public int points = 10;

    [Header("Reaction State")]
    public bool reactionStarted = false;
    public bool pointsAwarded = false;

    public void ActivateClue()
    {
        if (!goalAchieved) reactionStarted = false;        // a NEW activation can be reacted to again (points are still only given once)
        goalAchieved = true;
        Debug.Log($"[CLUE] {name}: goalAchieved = TRUE");
    }

    public void DeactivateClue()
    {
        goalAchieved = false;
        Debug.Log($"[CLUE] {name}: goalAchieved = FALSE");
    }
}