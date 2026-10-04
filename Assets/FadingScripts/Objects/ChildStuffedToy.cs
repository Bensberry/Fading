using UnityEngine;

// INT_Child_StuffedToy: the teddy bear on the child's bed.
// Touch: the teddy hops toward the pillow, into the child's arms.
// After 5 seconds it returns to its original position through SlideAndReturn.
// Moving the teddy activates the Teddy clue.
//
// Put this script on INT_Child_StuffedToy.
public class ChildStuffedToy : SlideAndReturn
{
    [Header("Teddy Clue")]
    public ClueGoal teddyClue;

    private bool clueActivated = false;
    private Vector3 originalPosition;
    private bool originalPositionSet = false;

    public override void ApplyDefaults()
    {
        prompt = "Nudge the teddy";
        slideDirection = Vector3.forward;
        distance = 0.25f;
        hopHeight = 0.06f;
    }

    private void Update()
    {
        // Store the position before the teddy is moved.
        if (!originalPositionSet)
        {
            originalPosition = transform.position;
            originalPositionSet = true;
        }

        // Detect when the teddy has actually moved.
        if (!clueActivated &&
            Vector3.Distance(transform.position, originalPosition) > 0.01f)
        {
            ActivateTeddyClue();
        }
    }

    private void ActivateTeddyClue()
    {
        clueActivated = true;

        if (teddyClue != null)
        {
            teddyClue.ActivateClue();
        }
        else
        {
            Debug.LogWarning(
                "ChildStuffedToy: Teddy ClueGoal is not assigned!",
                this
            );
        }
    }
}