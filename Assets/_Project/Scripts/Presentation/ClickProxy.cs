using TheLastCup.Core;
using UnityEngine;

namespace TheLastCup.Presentation
{
    public class ClickProxy : MonoBehaviour, IClickable
    {
        private IClickable _mainTarget;

        public void Setup(IClickable target)
        {
            _mainTarget = target;
        }

        public void OnHoverStart()
        {
            if (_mainTarget != null) _mainTarget.OnHoverStart();
        }

        public void OnHoverEnd()
        {
            if (_mainTarget != null) _mainTarget.OnHoverEnd();
        }

        public void OnClick()
        {
            if (_mainTarget != null) _mainTarget.OnClick();
        }
    }
}
