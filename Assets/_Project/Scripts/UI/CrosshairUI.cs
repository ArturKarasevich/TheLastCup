using TheLastCup.Core;
using UnityEngine;

namespace TheLastCup.UI
{
    public class CrosshairUI : MonoBehaviour
    {
        [SerializeField] private GameObject _crosshairImage;

        private void Awake()
        {
            if (!_crosshairImage)
                _crosshairImage = gameObject;
        }

        private void OnEnable()
        {
            GameEvents.OnCameraLockChanged += OnCameraLockChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnCameraLockChanged -= OnCameraLockChanged;
        }

        private void OnCameraLockChanged(bool isCameraLocked)
        {
            if (_crosshairImage)
                _crosshairImage.SetActive(!isCameraLocked);
        }
    }
}
