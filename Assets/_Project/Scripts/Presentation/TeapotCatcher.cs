using TheLastCup.Core;
using TheLastCup.Data;
using UnityEngine;

namespace TheLastCup.Presentation
{
    [RequireComponent(typeof(Collider))]
    public class TeapotCatcher : MonoBehaviour
    {
        private HerbInfo _lastCaughtHerb;

        private void Awake()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            var leaf = other.GetComponentInParent<FallingLeaf>();
            if (leaf != null && leaf.HerbData != null)
            {
                leaf.MarkCaught(transform);

                if (_lastCaughtHerb != leaf.HerbData)
                {
                    _lastCaughtHerb = leaf.HerbData;
                    GameEvents.RequestAddHerb(leaf.HerbData);
                    CancelInvoke(nameof(ResetCatch));
                    Invoke(nameof(ResetCatch), 0.6f);
                }
            }
        }

        private void ResetCatch()
        {
            _lastCaughtHerb = null;
        }
    }
}
