using UnityEngine;

public class SnakeAudio : MonoBehaviour
{
    [Header("Audio - SFX")]
    public AudioSource sfxSource;
    public AudioClip sfxMove;
    public AudioClip sfxCantMove;
    public AudioClip sfxFall;
    public AudioClip sfxEat;
    public AudioClip sfxDie;
    public AudioClip sfxGate;

    [Header("Audio - Music")]
    public AudioSource musicSource;
    public AudioClip musicClip;
    public AudioClip loseMusicClip;
    public AudioClip winMusicClip;

    private bool musicStarted = false;

    // ---------- SFX ----------
    public void PlayMove()      => PlaySFX(sfxMove);
    public void PlayCantMove()  => PlaySFX(sfxCantMove);
    public void PlayFall()      => PlaySFX(sfxFall);
    public void PlayEat()       => PlaySFX(sfxEat);
    public void PlayDieSFX()    => PlaySFX(sfxDie);
    public void PlayGateSFX()   => PlaySFX(sfxGate);

    void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }

    // ---------- MUSIC ----------
    public void StartMusicIfNeeded()
    {
        if (musicStarted) return;
        if (musicSource == null || musicClip == null) return;

        musicSource.clip = musicClip;
        musicSource.loop = true;
        musicSource.Play();
        musicStarted = true;
    }

    public void PlayLoseMusic()
    {
        if (musicSource == null || loseMusicClip == null) return;
        musicSource.clip = loseMusicClip;
        musicSource.loop = false;
        musicSource.Play();
    }

    public void PlayWinMusic()
    {
        if (musicSource == null || winMusicClip == null) return;
        musicSource.clip = winMusicClip;
        musicSource.loop = false;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
    }
}