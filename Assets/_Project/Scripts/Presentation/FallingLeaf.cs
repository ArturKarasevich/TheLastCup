using TheLastCup.Data;
using UnityEngine;

namespace TheLastCup.Presentation
{
    public class FallingLeaf : MonoBehaviour
    {
        public HerbInfo HerbData { get; private set; }

        public void Setup(HerbInfo data)
        {
            HerbData = data;
        }
    }
}
