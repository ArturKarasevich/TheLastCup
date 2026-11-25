using TheLastCup.Core;
using UnityEngine;

namespace TheLastCup.Presentation
{
    [RequireComponent(typeof(Collider))]
    public class TrashBucket : MonoBehaviour, IClickable
    {
        [SerializeField] private OutlineEffect _outline;

        private void Awake()
        {
            if (!_outline)
                _outline = GetComponent<OutlineEffect>();
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

            var teapot = FindFirstObjectByType<Teapot>();
            if (teapot)
                teapot.DumpTea();
        }
    }
}
