using System.Collections;

// Goes in: nowhere (it is the base of every cutscene). To make a new cutscene:
//   1. copy Cutscene_Morning.cs and rename the class inside it (keep the file name matching, it is tidier)
//   2. rewrite the steps inside Play(...)   (each step starts with "yield return c.")
//   3. play it from anywhere with:  CutsceneRunner.Play(new MyCutscene());
// 'c' (the CutsceneContext) has all the steps: Fade, Say, MoveCamera, MoveActor, Anim, Wait ... see CutsceneContext.cs.
// While a cutscene plays the player is frozen and Space skips it.
public abstract class Cutscene
{
    public abstract IEnumerator Play(CutsceneContext c);
}
