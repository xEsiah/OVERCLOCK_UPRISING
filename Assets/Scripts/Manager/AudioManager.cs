using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    public AudioSource sfxSource;
    public AudioSource musicSource;

    public AudioClip startSound;
    public AudioClip hoverSound;
    public AudioClip appearSound;
    public AudioClip ambientSound;
    public AudioClip portalLocalTeleport;
    public AudioClip portalSceneTransition;
    public AudioClip collectedItem;

    public AudioClip attackSound;
    public AudioClip disintegrationSound;
    public AudioClip integrationSound;
    public AudioClip jumpSound;
    public AudioClip dashSound;
    public AudioClip mantleSound;
    public AudioClip footstepWalkSound;
    public AudioClip footstepRunSound;

    public AudioClip menuMusic;

    private Coroutine _ambientCoroutine;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public void PlayStartSound() { if (sfxSource && startSound) sfxSource.PlayOneShot(startSound); }
    public void PlayHoverSound() { if (sfxSource && hoverSound) sfxSource.PlayOneShot(hoverSound); }
    public void PlayAppearSound() { if (sfxSource && appearSound) sfxSource.PlayOneShot(appearSound); }
    public void PlayAmbientSound() { if (sfxSource && ambientSound) sfxSource.PlayOneShot(ambientSound); }
    public void PlayPortalLocalTeleport() { if (sfxSource && portalLocalTeleport) sfxSource.PlayOneShot(portalLocalTeleport); }
    public void PlayPortalSceneTransition() { if (sfxSource && portalSceneTransition) sfxSource.PlayOneShot(portalSceneTransition); }
    public void PlayCollectedItem() { if (sfxSource && collectedItem) sfxSource.PlayOneShot(collectedItem); }
    
    public void PlayAttackSound() { if (sfxSource && attackSound) sfxSource.PlayOneShot(attackSound); }
    public void PlayDisintegrationSound() { if (sfxSource && disintegrationSound) sfxSource.PlayOneShot(disintegrationSound); }
    public void PlayIntegrationSound() { if (sfxSource && integrationSound) sfxSource.PlayOneShot(integrationSound); }
    public void PlayJumpSound() { if (sfxSource && jumpSound) sfxSource.PlayOneShot(jumpSound); }
    public void PlayDashSound() { if (sfxSource && dashSound) sfxSource.PlayOneShot(dashSound); }
    public void PlayMantleSound() { if (sfxSource && mantleSound) sfxSource.PlayOneShot(mantleSound); }
    public void PlayFootstepWalkSound() { if (sfxSource && footstepWalkSound) sfxSource.PlayOneShot(footstepWalkSound); }
    public void PlayFootstepRunSound() { if (sfxSource && footstepRunSound) sfxSource.PlayOneShot(footstepRunSound); }

    public float GetStartSoundLength() { return startSound != null ? startSound.length : 0f; }

    public void PlayMusic(AudioClip musicClip)
    {
        if (musicSource != null && musicClip != null)
        {
            if (musicSource.clip == musicClip && musicSource.isPlaying) return;
            musicSource.clip = musicClip;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    public void StartAmbientLoop(float delay)
    {
        StopAmbientLoop();
        _ambientCoroutine = StartCoroutine(AmbientRoutine(delay));
    }

    public void StopAmbientLoop()
    {
        if (_ambientCoroutine != null)
        {
            StopCoroutine(_ambientCoroutine);
        }
    }

    private IEnumerator AmbientRoutine(float delay)
    {
        while (true)
        {
            yield return new WaitForSeconds(delay);
            PlayAmbientSound();
        }
    }
}