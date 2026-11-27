using UnityEngine;

namespace TheLastCup.Presentation
{
    public class AmbienceAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _windSource;

        private void Start()
        {
            StartLoop(_musicSource);
            StartLoop(_windSource);
        }

        private void StartLoop(AudioSource source)
        {
            if (!source || !source.clip) return;

            source.loop = true;
            if (!source.isPlaying)
                source.Play();
        }
    }
}
