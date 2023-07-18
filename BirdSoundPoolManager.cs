using System;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class BirdSoundPoolManager : MonoBehaviour
{
    [System.Serializable]
    public class SpeciesEventMapping
    {
        public BirdSoundManager.SpeciesNames species;
        public EventReference fmodEvent;
    }

    public static BirdSoundPoolManager Instance { get; private set; }
    public int poolSize = 10;
    public List<SpeciesEventMapping> speciesEventMappings;
    private Dictionary<BirdSoundManager.SpeciesNames, EventReference> fmodEvents;
    private Dictionary<BirdSoundManager.SpeciesNames, Queue<EventInstance>> soundPools;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        fmodEvents = new Dictionary<BirdSoundManager.SpeciesNames, EventReference>();
        soundPools = new Dictionary<BirdSoundManager.SpeciesNames, Queue<EventInstance>>();
        foreach (SpeciesEventMapping mapping in speciesEventMappings)
        {
            fmodEvents[mapping.species] = mapping.fmodEvent;
            Queue<EventInstance> soundPool = new Queue<EventInstance>();
            for (int i = 0; i < poolSize; i++)
            {
                EventInstance soundInstance = RuntimeManager.CreateInstance(mapping.fmodEvent);
                soundPool.Enqueue(soundInstance);
            }
            soundPools[mapping.species] = soundPool;
        }
    }

    public EventInstance GetSound(BirdSoundManager.SpeciesNames species, Vector3 position)
    {
        EventInstance soundInstance;
        if (soundPools.ContainsKey(species) && soundPools[species].Count > 0)
        {
            soundInstance = soundPools[species].Dequeue();
        }
        else if (fmodEvents.ContainsKey(species))
        {
            soundInstance = RuntimeManager.CreateInstance(fmodEvents[species]);
        }
        else
        {
            Debug.LogError("No sound pool or FMOD event found for species: " + species);
            return new EventInstance();
        }
        FMOD.ATTRIBUTES_3D attributes = FMODUnity.RuntimeUtils.To3DAttributes(position);
        soundInstance.set3DAttributes(attributes);
        return soundInstance;
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