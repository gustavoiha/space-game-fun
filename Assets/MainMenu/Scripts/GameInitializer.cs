using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpaceGame.GameState
{
    public abstract class GameInitializationStrategy : ScriptableObject
    {
        public abstract IEnumerator Initialize();
    }

    public class GameInitializer : MonoBehaviour
    {
        [Header("Initialization")]
        [SerializeField] private GameInitializationStrategy initializationStrategy;

        [Header("Loading UI")]
        [SerializeField] private GameObject loadingScreen;
        [SerializeField] private UIDocument menuDocument;
        [SerializeField] private string newGameButtonName = "new-game-button";
        [SerializeField] private string loadingOverlayName = "loading-overlay";

        private bool _isInitializing;
        private Button _newGameButton;
        private VisualElement _loadingOverlay;

        private void OnEnable()
        {
            BindMenuElements();
            SetLoadingScreenVisible(false);
        }

        private void OnDisable()
        {
            if (_newGameButton != null)
            {
                _newGameButton.clicked -= StartGame;
            }
        }

        public void StartGame()
        {
            if (_isInitializing)
            {
                Debug.LogWarning("Game initialization is already in progress.");
                return;
            }

            if (initializationStrategy == null)
            {
                Debug.LogError("No initialization strategy assigned to GameInitializer.");
                return;
            }

            StartCoroutine(RunInitialization());
        }

        private IEnumerator RunInitialization()
        {
            _isInitializing = true;
            SetLoadingScreenVisible(true);

            yield return initializationStrategy.Initialize();

            SetLoadingScreenVisible(false);
            _isInitializing = false;
        }

        private void BindMenuElements()
        {
            if (menuDocument == null)
            {
                return;
            }

            VisualElement root = menuDocument.rootVisualElement;
            if (root == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(newGameButtonName))
            {
                _newGameButton = root.Q<Button>(newGameButtonName);
                if (_newGameButton != null)
                {
                    _newGameButton.clicked += StartGame;
                }
                else
                {
                    Debug.LogWarning($"New game button '{newGameButtonName}' was not found in the menu document.");
                }
            }

            if (!string.IsNullOrWhiteSpace(loadingOverlayName))
            {
                _loadingOverlay = root.Q(loadingOverlayName);
            }
        }

        private void SetLoadingScreenVisible(bool isVisible)
        {
            if (loadingScreen != null)
            {
                loadingScreen.SetActive(isVisible);
            }

            if (_loadingOverlay != null)
            {
                _loadingOverlay.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
