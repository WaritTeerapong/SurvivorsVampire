using UnityEngine;

public class CoreSceneManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SceneController.Instance
            .NewTransition()
            .Load(Slots.MAIN_MENU, Scenes.MAIN_MENU,setActive: true)
            .WithOverlay()
            .Perform();
    }

}
