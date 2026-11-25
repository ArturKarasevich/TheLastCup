using TheLastCup.Data;
using TheLastCup.Domain;
using UnityEngine;

namespace TheLastCup.Core
{
    public class GameLauncher : MonoBehaviour
    {
        [SerializeField] private SessionManager _sessionManager;
        [SerializeField] private bool _waitForMenu = false;

        private static GameLauncher _instance;
        public static GameDataStore CurrentGameData
        {
            get { return _instance != null ? _instance.GameData : null; }
        }

        public GameDataStore GameData { get; private set; }
        public ProgressManager PlayerProgress { get; private set; }

        private bool _hasSessionStarted;
        private bool _skipMenuOnce;

        private void OnEnable()
        {
            GameEvents.OnStartGameRequested += OnStartClicked;
        }

        private void OnDisable()
        {
            GameEvents.OnStartGameRequested -= OnStartClicked;
        }

        private void Awake()
        {
            _instance = this;
            _skipMenuOnce = GameEvents.SkipMenuNextTime;

            var dataLoader = new JsonLoader();
            var dataChecker = new DataChecker();

            GameData = new GameDataStore();
            GameData.LoadAllData(dataLoader, dataChecker);

            PlayerProgress = new ProgressManager();

            if (!_sessionManager)
                _sessionManager = GetComponent<SessionManager>();
        }

        private void Start()
        {
            GameEvents.SkipMenuNextTime = false;

            if (!_waitForMenu || _skipMenuOnce)
                StartGame();
        }

        private void OnStartClicked()
        {
            StartGame();
        }

        private void StartGame()
        {
            if (_hasSessionStarted) return;
            _hasSessionStarted = true;

            if (_sessionManager)
                _sessionManager.Setup(GameData, PlayerProgress);
        }
    }
}
