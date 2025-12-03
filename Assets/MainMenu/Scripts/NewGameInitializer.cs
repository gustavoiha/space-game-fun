using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceGame.GameState
{
    [CreateAssetMenu(menuName = "Game State/New Game Initializer", fileName = "NewGameInitializer")]
    public class NewGameInitializer : GameInitializationStrategy
    {
        [SerializeField] private string initialSceneName = "SpaceSandbox";

        public override IEnumerator Initialize()
        {
            if (string.IsNullOrWhiteSpace(initialSceneName))
            {
                Debug.LogError("Initial scene name must be configured before starting a new game.");
                yield break;
            }

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(initialSceneName, LoadSceneMode.Single);
            if (loadOperation == null)
            {
                Debug.LogError($"Failed to load initial scene '{initialSceneName}'.");
                yield break;
            }

            while (!loadOperation.isDone)
            {
                yield return null;
            }
        }
    }
}
