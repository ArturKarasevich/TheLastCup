using System;
using TheLastCup.Core;
using TMPro;
using UnityEngine;

namespace TheLastCup.Presentation
{
    [RequireComponent(typeof(Collider))]
    public class TalkChoiceButton : MonoBehaviour, IClickable
    {
        [SerializeField] private TMP_Text _buttonText;
        [SerializeField] private OutlineEffect _outline;

        private Action _onClickAction;

        private void Awake()
        {
            if (!_outline)
                _outline = GetComponent<OutlineEffect>();
        }

        public void Setup(string text, Action onClick)
        {
            if (_buttonText)
                _buttonText.text = text;

            _onClickAction = onClick;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            _onClickAction = null;
            if (_outline)
                _outline.SetLit(false);
            gameObject.SetActive(false);
        }

        public void OnHoverStart()
        {
            if (_outline)
                _outline.SetLit(true);
        }

        public void OnHoverEnd()
        {
            if (_outline)
                _outline.SetLit(false);
        }

        public void OnClick()
        {
            if (_outline)
                _outline.SetLit(false);

            if (_onClickAction != null)
                _onClickAction.Invoke();
        }
    }
}
