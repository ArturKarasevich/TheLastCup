using TheLastCup.Core;
using UnityEngine;

namespace TheLastCup.Presentation
{
    [RequireComponent(typeof(Collider))]
    public class TeapotCatcher : MonoBehaviour
    {
        private bool _caughtThisFall;

        private void Awake()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            var leaf = other.GetComponent<FallingLeaf>();
            if (leaf != null && leaf.HerbData != null)
            {
                if (!_caughtThisFall)
                {
                    _caughtThisFall = true;
                    GameEvents.RequestAddHerb(leaf.HerbData);
                    Invoke(nameof(ResetCatch), 0.6f);
                }
            }
        }

        private void ResetCatch()
        {
            _caughtThisFall = false;
        }
    }
}
