using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class BirdSoundPoolManager : MonoBehaviour
{
    public static BirdSoundPoolManager Instance { get; private set; }

    public int poolSize = 10;

    [System.Serializable]
    public class SpeciesSound
    {
        public string speciesName;
        public EventReference fmodEvent;
    }

    public List<SpeciesSound> speciesSounds; // List of species and their corresponding FMOD events

    private Dictionary<string, Queue<EventInstance>> soundPools; // Dictionary of sound pools for each species

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            soundPools = new Dictionary<string, Queue<EventInstance>>();

            foreach (var speciesSound in speciesSounds)
            {
                Queue<EventInstance> soundPool = new Queue<EventInstance>();
                for (int i = 0; i < poolSize; i++)
                {
                    EventInstance soundInstance = RuntimeManager.CreateInstance(speciesSound.fmodEvent);
                    soundPool.Enqueue(soundInstance);
                }
                soundPools[speciesSound.speciesName] = soundPool;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public EventInstance GetSound(string species)
    {
        if (soundPools[species].Count > 0)
        {
            return soundPools[species].Dequeue();
        }
        else
        {
            Debug.LogWarning("Sound pool for species " + species + " is empty. Waiting for a sound to be returned to the pool.");
            return new EventInstance(); // Return a default EventInstance if the pool is empty
        }
    }

    public void ReturnSound(string species, EventInstance soundInstance)
    {
        soundPools[species].Enqueue(soundInstance);
    }

    private void OnDestroy()
    {
        foreach (var soundPool in soundPools.Values)
        {
            while (soundPool.Count > 0)
            {
                EventInstance soundInstance = soundPool.Dequeue();
                soundInstance.release();
            }
        }
    }
}
