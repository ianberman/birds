using System;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class BirdSoundPoolManager : MonoBehaviour
{
    public static BirdSoundPoolManager Instance { get; private set; }
    public int poolSize = 10;
    public Dictionary<BirdSoundManager.SpeciesNames, EventReference> fmodEvents;
    private Dictionary<BirdSoundManager.SpeciesNames, Queue<EventInstance>> soundPools;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            fmodEvents = new Dictionary<BirdSoundManager.SpeciesNames, EventReference>();
            soundPools = new Dictionary<BirdSoundManager.SpeciesNames, Queue<EventInstance>>();
            foreach (BirdSoundManager.SpeciesNames species in Enum.GetValues(typeof(BirdSoundManager.SpeciesNames)))
            {
                if (BirdSoundManager.Instance.TryGetFMODEventForSpecies(species, out EventReference fmodEvent))
                {
                    fmodEvents[species] = fmodEvent;
                    Queue<EventInstance> soundPool = new Queue<EventInstance>();
                    for (int i = 0; i < poolSize; i++)
                    {
                        EventInstance soundInstance = RuntimeManager.CreateInstance(fmodEvent);
                        soundPool.Enqueue(soundInstance);
                    }
                    soundPools[species] = soundPool;
                }
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public EventInstance GetSound(BirdSoundManager.SpeciesNames species)
    {
        if (soundPools[species].Count > 0)
        {
            return soundPools[species].Dequeue();
        }
        else
        {
            EventInstance soundInstance = RuntimeManager.CreateInstance(fmodEvents[species]);
            return soundInstance;
        }
    }

    public void ReturnSound(BirdSoundManager.SpeciesNames species, EventInstance soundInstance)
    {
        soundPools[species].Enqueue(soundInstance);
    }


    private void OnDestroy()
    {
        foreach (Queue<EventInstance> soundPool in soundPools.Values)
        {
            while (soundPool.Count > 0)
            {
                EventInstance soundInstance = soundPool.Dequeue();
                soundInstance.release();
            }
        }
    }


}