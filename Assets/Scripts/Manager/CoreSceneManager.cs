using UnityEngine;

public class CoreSceneManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SceneController.Instance
            .NewTransition()
            .Load(SceneDatabase.Slots.MAIN_MENU, SceneDatabase.Scenes.MAIN_MENU)
            .WithOverlay()
            .Perform();
    }

}
