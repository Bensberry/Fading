using System.Collections;

// Goes in: nowhere (it is the base of every cutscene). To make a new cutscene:
//   1. copy one of the classes in StoryCutscenes.cs and rename it
//   2. rewrite the steps inside Play(...)   (each step starts with "yield return c.")
//   3. play it from anywhere with:  CutsceneRunner.Play(new MyCutscene());
// 'c' (the CutsceneContext) has all the steps: Fade, Say, MoveCamera, Spawn, MoveActor, Wait ... see CutsceneContext.cs.
// While a cutscene plays the player is frozen and Space skips it.
public abstract class Cutscene
{
    public abstract IEnumerator Play(CutsceneContext c);
}
