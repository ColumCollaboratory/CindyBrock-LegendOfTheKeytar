using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using BattleRoyalRhythm.GridActors;

public class BackToMenu : GridTrigger
{

    protected override sealed void OnActorEnter(GridActor actorEntered)
    {
        SceneManager.LoadScene(0);
    }
}
