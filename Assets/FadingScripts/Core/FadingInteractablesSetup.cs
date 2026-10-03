using UnityEngine;

// One-click setup: put this on the FadingHouse object and press Play.
// It finds every INT_ object by name and adds that object's own script (MomCoffeeMug, GrandmaClock, ...).
// Objects that already have a script (added by hand) are left alone.
// Drag your sounds into the slots below; every slot is optional.
public class FadingInteractablesSetup : MonoBehaviour
{
    [Header("Hallway")]
    public AudioClip candleWhoosh;
    public AudioClip photoTap;
    public AudioClip calendarRustle;
    [Header("Grandma")]
    public AudioClip chairCreak;
    public AudioClip lampBuzz;
    public AudioClip albumPage;
    public AudioClip clockChime;
    [Header("Mom")]
    public AudioClip radioSong;
    public float radioSongStart = 0f;
    public AudioClip mugSlide;
    public AudioClip fogSqueak;
    public string fogNickname = "Sunny";
    [Header("Child")]
    public AudioClip lullaby;
    public AudioClip toyThump;
    public AudioClip nightlightClick;
    public AudioClip mobileChime;
    [Header("Doors")]
    public AudioClip doorCreak;

    void Awake()
    {
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (!t.name.StartsWith("INT_") || t.GetComponent<Interactable>() != null) continue;
            GameObject g = t.gameObject;
            switch (t.name)
            {
                case "INT_Hallway_MemorialCandle": Add<MemorialCandle>(g, candleWhoosh); break;
                case "INT_Hallway_FamilyPhoto":    Add<FamilyPhoto>(g, photoTap); break;
                case "INT_Hallway_Calendar":       Add<HallwayCalendar>(g, calendarRustle); break;

                case "INT_Grandma_RockingChair":
                    // The Chapter0 scene already has its own rocking chair; don't make a second touchable one.
                    if (FindFirstObjectByType<GrandmaRockingChair>() == null) Add<GrandmaRockingChair>(g, chairCreak);
                    break;
                case "INT_Grandma_Lamp":           Add<GrandmaLamp>(g, lampBuzz); break;
                case "INT_Grandma_PhotoAlbum":     Add<GrandmaPhotoAlbum>(g, albumPage); break;
                case "INT_Grandma_Clock":          Add<GrandmaClock>(g, clockChime); break;

                case "INT_Mom_Radio":
                {
                    MomRadio r = Add<MomRadio>(g, null);
                    r.song = radioSong; r.songStart = radioSongStart; break;
                }
                case "INT_Mom_CoffeeMug":          Add<MomCoffeeMug>(g, mugSlide); break;
                case "INT_Mom_TablePhoto":         Add<MomTablePhoto>(g, photoTap); break;
                case "INT_Mom_FogWindow":
                {
                    MomFogWindow f = Add<MomFogWindow>(g, fogSqueak);
                    f.nickname = fogNickname; break;
                }

                case "INT_Child_MusicBox":
                {
                    ChildMusicBox m = Add<ChildMusicBox>(g, null);
                    m.lullaby = lullaby; break;
                }
                case "INT_Child_StuffedToy":       Add<ChildStuffedToy>(g, toyThump); break;
                case "INT_Child_Nightlight":       Add<ChildNightlight>(g, nightlightClick); break;
                case "INT_Child_Mobile":           Add<ChildMobile>(g, mobileChime); break;

                case "INT_Door_Front":             Add<FrontDoor>(g, doorCreak); break;
                case "INT_Door_Guest":             Add<GuestRoomDoor>(g, doorCreak); break;
                case "INT_Door_Child":             Add<ChildRoomDoor>(g, doorCreak); break;
                case "INT_Door_Mother":            Add<MotherRoomDoor>(g, doorCreak); break;
            }
        }
    }

    static T Add<T>(GameObject g, AudioClip clip) where T : Interactable
    {
        T i = g.AddComponent<T>();
        i.ApplyDefaults();
        if (clip != null) i.sound = clip;
        return i;
    }
}
