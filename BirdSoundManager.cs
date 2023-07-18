using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class BirdSoundManager : MonoBehaviour
{
    [SerializeField]
    private List<Zone> zones; // List of zones, each with their possible bird species

    // Master list of bird species and their respective FMOD events
    [SerializeField]
    private List<BirdSpecies> birdSpecies;

    // BirdSoundEmitter references in the game world
    private BirdSoundEmitter[] birdSoundEmitters;

    [System.Serializable]
    public class Zone
    {
        public ZoneNames zoneName;
        public List<SpeciesNames> possibleBirdSpecies; // List of bird species names allowed in this zone
    }

    public enum SpeciesNamesWithDefault
    {
        Unassigned,
        HouseSparrow_Calm,
        HouseSparrow_Busy,
        RufousCollaredSparrow,
        NormalBird,
        EaredDove
    }

    [System.Serializable]
    public class BirdSpecies
    {
        public SpeciesNames speciesName;
        public EventReference fmodEvent;
        public int TotalClips;
    }

    public enum ZoneNames
    {
        Park,
        SchoolArea,
        CityArea
    }

    public enum SpeciesNames
    {
        ChirpyBird,
        CoolBird,
        EaredDove,
        GreatThrush,
        HouseSparrow_Busy,
        HouseSparrow_Calm,
        LesbiaVictoriae,
        NighttimeBug,
        NormalBird,
        RufousCollaredSparrow,
        TanagersVarious
    }

    private void Awake()
    {
        // Populate birdSoundEmitters array with BirdSoundEmitter scripts in the game world
        birdSoundEmitters = GameObject.FindObjectsOfType<BirdSoundEmitter>();

        // Assign BirdSoundManager reference and populate available species list in each emitter
        foreach (BirdSoundEmitter emitter in birdSoundEmitters)
        {
            emitter.AssignBirdSoundManager(this);
            emitter.PopulateSpeciesList();
        }

        // Initialize TotalClips for each bird species
        foreach (BirdSpecies species in birdSpecies)
        {
            FMOD.Studio.EventInstance eventInstance = RuntimeManager.CreateInstance(species.fmodEvent);

            // Set 3D attributes here
            FMOD.ATTRIBUTES_3D attributes = FMODUnity.RuntimeUtils.To3DAttributes(Vector3.zero); // Use Vector3.zero or any other suitable position
            eventInstance.set3DAttributes(attributes);

            FMOD.Studio.EventDescription eventDescription;
            eventInstance.getDescription(out eventDescription);

            FMOD.Studio.PARAMETER_DESCRIPTION parameterDescription;
            eventDescription.getParameterDescriptionByName("TotalClips", out parameterDescription);

            float totalClips;
            eventInstance.getParameterByID(parameterDescription.id, out totalClips);

            eventInstance.release();

            species.TotalClips = Mathf.RoundToInt(totalClips);
        }
    }


    // Get the FMOD event for a given bird species name
    public bool TryGetFMODEventForSpecies(SpeciesNames speciesName, out EventReference fmodEvent)
    {
        foreach (BirdSpecies species in birdSpecies)
        {
            if (species.speciesName == speciesName)
            {
                fmodEvent = species.fmodEvent;
                return true;
            }
        }

        fmodEvent = default(EventReference);
        return false;
    }

    // Get the list of possible bird species for a given zone name
    public List<SpeciesNames> GetPossibleSpeciesForZone(ZoneNames zoneName)
    {
        foreach (Zone zone in zones)
        {
            if (zone.zoneName == zoneName)
            {
                return zone.possibleBirdSpecies;
            }
        }
        return null;
    }

    // Get the total number of clips for a given bird species
    public int GetTotalClipsForSpecies(SpeciesNames speciesName)
    {
        foreach (BirdSpecies species in birdSpecies)
        {
            if (species.speciesName == speciesName)
            {
                return species.TotalClips;
            }
        }

        Debug.LogWarning("Could not find bird species: " + speciesName);
        return 0;
    }
}