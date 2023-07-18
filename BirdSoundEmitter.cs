using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections;
using System;

public class BirdSoundEmitter : MonoBehaviour
{
    private bool isEligibleToPlay = true;

    private static Dictionary<IntPtr, BirdSoundEmitter> instanceToEmitter = new Dictionary<IntPtr, BirdSoundEmitter>();

    private FMOD.Studio.EVENT_CALLBACK soundCompletedCallback;

    [SerializeField]
    private BirdSoundManager.ZoneNames zoneName; // The zone this emitter belongs to

    private BirdSoundManager birdSoundManager;
    private List<BirdSoundManager.SpeciesNames> availableSpecies;

    [SerializeField]
    private float minDelay = 0f; // Minimum delay between sound clips
    [SerializeField]
    private float maxDelay = 30f; // Maximum delay between sound clips

    [SerializeField]
    private float silenceChance = 0.5f; // Chance of playing silence
    [SerializeField]
    private float minSilenceDuration = 30f; // Minimum silence duration
    [SerializeField]
    private float maxSilenceDuration = 180f; // Maximum silence duration

    //[SerializeField]
    //private float exclusionRadius = 5f; // Exclusion radius for other emitters

    [SerializeField]
    private float switchSpeciesChance = 0.5f; // Chance of switching species

    private EventInstance birdSoundInstance; // FMOD EventInstance for bird sounds
    private BirdSoundManager.SpeciesNames currentSpecies; // Currently playing bird species
    private int totalClipsPlayed; // Total clips played for the current species
    private int totalClipsForSpecies; // Total clips for the current species

    private BirdSoundManager manager;

    [SerializeField]
    private BirdSoundManager.SpeciesNamesWithDefault initialSpecies = BirdSoundManager.SpeciesNamesWithDefault.Unassigned;

    private void Start()
    {
        soundCompletedCallback = new FMOD.Studio.EVENT_CALLBACK(SoundCompletedCallback);

        if (availableSpecies != null && availableSpecies.Count > 0)
        {
            if (initialSpecies != BirdSoundManager.SpeciesNamesWithDefault.Unassigned && availableSpecies.Contains((BirdSoundManager.SpeciesNames)initialSpecies))
            {
                // If the initial species is assigned and in the available list, play it
                StartCoroutine(PlayBirdSound((BirdSoundManager.SpeciesNames)initialSpecies));
            }
            else
            {
                // Otherwise, continue as before
                float roll = UnityEngine.Random.value;
                if (roll <= silenceChance)
                {
                    float initialSilenceDuration = UnityEngine.Random.Range(minSilenceDuration, maxSilenceDuration);
                    StartCoroutine(InitialSilence(initialSilenceDuration));
                }
                else
                {
                    StartCoroutine(CheckSoundState());
                }
            }
        }
        else
        {
            Debug.LogWarning("No available species for BirdSoundEmitter.");
        }
    }

    private IEnumerator InitialSilence(float duration)
    {
        yield return new WaitForSeconds(duration);
        StartCoroutine(CheckSoundState());
    }


    private IEnumerator CheckSoundState()
    {
        while (true)
        {
            if (isEligibleToPlay)
            {
                PlayRandomSpecies();
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    // Assign the BirdSoundManager to the emitter
    public void AssignBirdSoundManager(BirdSoundManager manager)
    {
        birdSoundManager = manager;
    }

    // Populate the available species list based on the zone
    public void PopulateSpeciesList()
    {
        availableSpecies = birdSoundManager.GetPossibleSpeciesForZone(zoneName);
    }

    // Play a random bird species
    private void PlayRandomSpecies()
    {
        StartCoroutine(DelayIneligibility());
        currentSpecies = availableSpecies[UnityEngine.Random.Range(0, availableSpecies.Count)];
        totalClipsForSpecies = birdSoundManager.GetTotalClipsForSpecies(currentSpecies);
        totalClipsPlayed = 0;
        StartCoroutine(PlayBirdSound(currentSpecies));
    }

    private IEnumerator DelayIneligibility()
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(minDelay, maxDelay));
        isEligibleToPlay = false;
    }


    // Play a bird sound for the given species
    private IEnumerator PlayBirdSound(BirdSoundManager.SpeciesNames species)
    {
        if (birdSoundManager.TryGetFMODEventForSpecies(species, out EventReference fmodEvent))
        {
            if (birdSoundInstance.isValid())
            {
                PLAYBACK_STATE state;
                birdSoundInstance.getPlaybackState(out state);
                if (state == PLAYBACK_STATE.PLAYING)
                {
                    // If the instance is still playing, delay the start of the new sound
                    yield return new WaitForSeconds(UnityEngine.Random.Range(minDelay, maxDelay));
                    StartCoroutine(PlayBirdSound(species));
                    yield break;
                }
                else
                {
                    birdSoundInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                    instanceToEmitter.Remove(birdSoundInstance.handle); // Remove from dictionary
                    // Return the sound instance to the pool instead of releasing it
                    BirdSoundPoolManager.Instance.ReturnSound(species.ToString(), birdSoundInstance);

                }
            }

            // Get a sound instance from the pool instead of creating a new one
            birdSoundInstance = BirdSoundPoolManager.Instance.GetSound(species.ToString());
            if (!birdSoundInstance.isValid())
            {
                Debug.LogWarning("Could not get sound instance for species: " + species);
                yield break;
            }
            instanceToEmitter[birdSoundInstance.handle] = this;

            // Set 3D attributes
            FMOD.ATTRIBUTES_3D attributes = FMODUnity.RuntimeUtils.To3DAttributes(gameObject.transform.position);
            birdSoundInstance.set3DAttributes(attributes);

            birdSoundInstance.setCallback(soundCompletedCallback, FMOD.Studio.EVENT_CALLBACK_TYPE.STOPPED);
            birdSoundInstance.start();

            // Rest of the function...

            totalClipsPlayed++;

            if (totalClipsPlayed >= totalClipsForSpecies)
            {
                float roll = UnityEngine.Random.value;

                // Play silence with a chance of 'silenceChance'
                if (roll <= silenceChance)
                {
                    float silenceDuration = UnityEngine.Random.Range(minSilenceDuration, maxSilenceDuration);
                    yield return new WaitForSeconds(silenceDuration);
                    PlayRandomSpecies();
                }
                // Switch to a new random species with a chance of 'switchSpeciesChance'
                else if (roll <= switchSpeciesChance + silenceChance)
                {
                    PlayRandomSpecies();
                }
                else
                {
                    totalClipsPlayed = 0;
                    yield return new WaitForSeconds(UnityEngine.Random.Range(minDelay, maxDelay));
                    StartCoroutine(PlayBirdSound(species));
                }
            }
            else
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(minDelay, maxDelay));
                StartCoroutine(PlayBirdSound(species));
            }
        }
        else
        {
            Debug.LogWarning("Could not find FMOD event for species: " + species);
        }
    }


    // Check if the emitter is currently playing a sound
    public bool IsCurrentlyPlaying()
    {
        if (birdSoundInstance.isValid())
        {
            PLAYBACK_STATE playbackState;
            birdSoundInstance.getPlaybackState(out playbackState);
            return playbackState == PLAYBACK_STATE.PLAYING;
        }

        return false;
    }

    // Stop the sound playback for a given duration
    public void StopPlayback(float minStopDuration, float maxStopDuration)
    {
        if (birdSoundInstance.isValid())
        {
            birdSoundInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);

            float stopDuration = UnityEngine.Random.Range(minStopDuration, maxStopDuration);
            StartCoroutine(ResumePlaybackAfterDelay(stopDuration));
        }
    }

    private IEnumerator ResumePlaybackAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ResumePlayback();
    }


    // Resume playback after stopping
    private void ResumePlayback()
    {
        if (totalClipsPlayed < totalClipsForSpecies)
        {
            StartCoroutine(PlayBirdSound(currentSpecies));
        }
        else
        {
            PlayRandomSpecies();
        }
    }

    // Call this method when the player interacts with the emitter
    public void PlayerInteraction(float minStopDuration, float maxStopDuration)
    {
        StopPlayback(minStopDuration, maxStopDuration);
    }

    [AOT.MonoPInvokeCallback(typeof(FMOD.Studio.EVENT_CALLBACK))]
    static FMOD.RESULT SoundCompletedCallback(FMOD.Studio.EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr parameterPtr)
    {
        if (type == FMOD.Studio.EVENT_CALLBACK_TYPE.STOPPED)
        {
            if (instanceToEmitter.TryGetValue(instancePtr, out BirdSoundEmitter emitter))
            {
                emitter.isEligibleToPlay = true;
                // Return the sound instance to the pool
                BirdSoundPoolManager.Instance.ReturnSound(emitter.birdSoundInstance);
            }
        }
        return FMOD.RESULT.OK;
    }

}