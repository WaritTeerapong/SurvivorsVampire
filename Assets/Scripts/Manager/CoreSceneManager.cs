using UnityEngine;

public class CoreSceneManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (SceneController.Instance != null)
        {
            string activeSceneName = SceneController.Instance.GetActiveSceneName();

            Debug.Log(activeSceneName);

            // Test mode
            if (activeSceneName != "CoreScene")
            {
                SceneController.Instance
                    .NewTransition()
                    .SetSlotInTestMode(Slots.SESSION, activeSceneName)
                    .Perform();
                return;
            }

            // Normal mode
            SceneController.Instance
                .NewTransition()
                .Load(Slots.MAIN_MENU, Scenes.MAIN_MENU, setActive: true)
                .WithOverlay()
                .Perform();
        }
    }
}
