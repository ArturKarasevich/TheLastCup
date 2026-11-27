using UnityEngine;

namespace TheLastCup.Core
{
    public static class AudioHelper
    {
        public static void Play(AudioSource source)
        {
            if (source && source.clip)
                source.PlayOneShot(source.clip);
        }
    }
}
