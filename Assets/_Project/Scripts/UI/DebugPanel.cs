using TheLastCup.Core;
using TheLastCup.Data;
using TheLastCup.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TheLastCup.UI
{
    public class DebugPanel : MonoBehaviour
    {
        [SerializeField] private SessionManager _sessionManager;
        [SerializeField] private bool _showInBuild = true;

        private bool _isOpen;
        private bool _showFps;
        private Rect _windowRect = new Rect(20, 20, 320, 460);

        private void Awake()
        {
            if (!_sessionManager)
                _sessionManager = FindFirstObjectByType<SessionManager>();
        }

        private void Update()
        {
            if (!_showInBuild && !Application.isEditor) return;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.f1Key.wasPressedThisFrame)
                {
                    _isOpen = !_isOpen;
                    GameEvents.SetUIMode(_isOpen);
                }

                if (Keyboard.current.f2Key.wasPressedThisFrame)
                    _showFps = !_showFps;
            }
        }

        private void OnGUI()
        {
            if (!_showInBuild && !Application.isEditor) return;

            if (_showFps)
            {
                int fps = Time.unscaledDeltaTime > 0f ? Mathf.RoundToInt(1f / Time.unscaledDeltaTime) : 0;
                GUI.Box(new Rect(Screen.width - 110, 12, 98, 26), $"{fps} FPS");
            }

            if (!_isOpen) return;

            _windowRect = GUI.Window(9001, _windowRect, DrawDebugWindow, "QA Тест-панель (F1 — закрыть)");
        }

        private void DrawDebugWindow(int id)
        {
            if (!_sessionManager)
            {
                GUILayout.Label("SessionManager не найден!");
                GUI.DragWindow();
                return;
            }

            var spirit = _sessionManager.CurrentGuest;
            var prog = _sessionManager.Progress;

            GUILayout.Label($"Текущий гость: {(spirit != null ? spirit.name : "нет")}");
            if (prog != null)
            {
                GUILayout.Label($"Свет: {prog.Light} | Перелом: {(prog.HasStoryTwist ? "ДА" : "нет")}");
                GUILayout.Label($"Свисток: {prog.OwnsWhistle} | Камертон: {prog.OwnsTuningFork} | Лупа: {prog.OwnsMagnifier}");
            }

            GUILayout.Space(8);
            GUILayout.Label("<b>Перейти к гостю:</b>");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("1. Фёдор")) _sessionManager.SkipToGuest("fedor");
            if (GUILayout.Button("2. Лина")) _sessionManager.SkipToGuest("lina");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("3. Григорий")) _sessionManager.SkipToGuest("grigory");
            if (GUILayout.Button("4. Игнат")) _sessionManager.SkipToGuest("ignat");
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("<b>Сразу подать чай текущему гостю:</b>");
            if (GUILayout.Button("Идеальный чай (3/3)"))
                _sessionManager.TestTeaCheck(3);
            if (GUILayout.Button("Средний чай (2/3)"))
                _sessionManager.TestTeaCheck(2);
            if (GUILayout.Button("Плохой чай (0/3)"))
                _sessionManager.TestTeaCheck(0);

            GUILayout.Space(8);
            GUILayout.Label("<b>Инструменты и читы:</b>");
            if (GUILayout.Button("Телепорт гостя в итоговую точку"))
                GameEvents.DebugSnapGuest();

            if (GUILayout.Button("Скип диалога до выбора / конца"))
                GameEvents.DebugSkipTalk();

            if (GUILayout.Button("Засыпать нужные травы в чайник"))
                FillTeapotWithRightHerbs(spirit);

            if (GUILayout.Button("Выдать все артефакты"))
                _sessionManager.TestGiveAllItems();

            if (GUILayout.Button("+2 Света"))
            {
                if (prog != null)
                {
                    prog.AddLight(2);
                    GameEvents.SendLightChanged(prog.Light);
                }
            }

            if (GUILayout.Button("Закрыть панель (F1)"))
            {
                _isOpen = false;
                GameEvents.SetUIMode(false);
            }

            GUI.DragWindow();
        }

        private void FillTeapotWithRightHerbs(GuestInfo spirit)
        {
            if (spirit == null || spirit.wantedHerbs == null || _sessionManager == null || _sessionManager.GameData == null) return;

            foreach (var herbId in spirit.wantedHerbs)
            {
                HerbInfo herb = _sessionManager.GameData.GetHerb(herbId);
                if (herb != null)
                    GameEvents.RequestAddHerb(herb);
            }
        }
    }
}
