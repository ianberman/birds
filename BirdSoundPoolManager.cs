using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class BirdSoundPoolManager : MonoBehaviour
{
    public static BirdSoundPoolManager Instance { get; private set; }

    public int poolSize = 10;
    public EventReference fmodEvent;
    private Queue<EventInstance> soundPool;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            soundPool = new Queue<EventInstance>();

            for (int i = 0; i < poolSize; i++)
            {
                EventInstance soundInstance = RuntimeManager.CreateInstance(fmodEvent);
                soundPool.Enqueue(soundInstance);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public EventInstance GetSound()
    {
        if (soundPool.Count > 0)
        {
            return soundPool.Dequeue();
        }
        else
        {
            // If pool is empty, create a new sound instance
            EventInstance soundInstance = RuntimeManager.CreateInstance(fmodEvent);
            return soundInstance;
        }
    }

    public void ReturnSound(EventInstance soundInstance)
    {
        soundPool.Enqueue(soundInstance);
    }

    private void OnDestroy()
    {
        while (soundPool.Count > 0)
        {
            EventInstance soundInstance = soundPool.Dequeue();
            soundInstance.release();
        }
    }

}