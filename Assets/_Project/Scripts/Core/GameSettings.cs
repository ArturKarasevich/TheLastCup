using UnityEngine;

namespace TheLastCup.Core
{
    public static class GameSettings
    {
        private const string VolumeKey = "TLC_MasterVolume";
        private const string TextSpeedKey = "TLC_SubtitleSpeed";
        private const string SensitivityKey = "TLC_CameraSensitivityMult";
        private const string InvertYKey = "TLC_InvertY";
        private const string FullscreenKey = "TLC_Fullscreen";

        private static bool _isLoaded;
        private static float _volume = 1f;
        private static float _textSpeed = 45f;
        private static float _sensitivity = 1f;
        private static bool _invertY;
        private static bool _fullscreen = true;

        public static float MasterVolume
        {
            get { LoadSettings(); return _volume; }
            set
            {
                LoadSettings();
                _volume = Mathf.Clamp01(value);
                AudioListener.volume = _volume;
                PlayerPrefs.SetFloat(VolumeKey, _volume);
            }
        }

        public static float SubtitleSpeed
        {
            get { LoadSettings(); return _textSpeed; }
            set
            {
                LoadSettings();
                _textSpeed = Mathf.Clamp(value, 20f, 120f);
                PlayerPrefs.SetFloat(TextSpeedKey, _textSpeed);
            }
        }

        public static float CameraSensitivity
        {
            get { LoadSettings(); return _sensitivity; }
            set
            {
                LoadSettings();
                _sensitivity = Mathf.Clamp(value, 0.3f, 2.5f);
                PlayerPrefs.SetFloat(SensitivityKey, _sensitivity);
            }
        }

        public static bool InvertY
        {
            get { LoadSettings(); return _invertY; }
            set
            {
                LoadSettings();
                _invertY = value;
                PlayerPrefs.SetInt(InvertYKey, _invertY ? 1 : 0);
            }
        }

        public static bool Fullscreen
        {
            get { LoadSettings(); return _fullscreen; }
            set
            {
                LoadSettings();
                _fullscreen = value;
                Screen.fullScreen = _fullscreen;
                PlayerPrefs.SetInt(FullscreenKey, _fullscreen ? 1 : 0);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void LoadOnStart()
        {
            LoadSettings();
        }

        public static void LoadSettings()
        {
            if (_isLoaded) return;
            _isLoaded = true;

            _volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
            _textSpeed = Mathf.Clamp(PlayerPrefs.GetFloat(TextSpeedKey, 45f), 20f, 120f);
            _sensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(SensitivityKey, 1f), 0.3f, 2.5f);
            _invertY = PlayerPrefs.GetInt(InvertYKey, 0) == 1;
            _fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;

            AudioListener.volume = _volume;
            Screen.fullScreen = _fullscreen;
        }

        public static void SaveSettings()
        {
            PlayerPrefs.Save();
        }
    }
}
