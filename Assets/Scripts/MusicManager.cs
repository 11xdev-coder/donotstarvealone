using UnityEngine;
using System.Collections;

public class MusicManager : MonoBehaviour
{
    public AudioSource musicSource; 
    public MusicManager Instance { get; private set; }

    [Tooltip("Transition time between musics in seconds")]
    public float transitionTime = 1f;
    public float targetVolume = 1f;
    private Coroutine _currentTransitionCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject); 
        }
    }

    public void ChangeCurrentMusic(AudioClip newMusicClip)
    {
        if (musicSource.clip == newMusicClip) return;

        // Stop the current coroutine if it's running
        if (_currentTransitionCoroutine != null)
        {
            StopCoroutine(_currentTransitionCoroutine);
        }

        _currentTransitionCoroutine = StartCoroutine(TransitionMusic(newMusicClip));
    }

    private IEnumerator TransitionMusic(AudioClip newMusicClip)
    {
        // Fade out the current music
        float currentTime = 0;
        while (currentTime < transitionTime)
        {
            currentTime += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(targetVolume, 0, currentTime / transitionTime);
            yield return null;
        }

        // Change music clip and start playing new music
        musicSource.clip = newMusicClip;
        musicSource.Play();

        // Fade in the new music
        currentTime = 0;
        while (currentTime < transitionTime)
        {
            currentTime += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0, targetVolume, currentTime / transitionTime);
            yield return null;
        }
    }
}