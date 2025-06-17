using UnityEngine;

[CreateAssetMenu(fileName = "NewSoundtrack", menuName = "Plant Game/Soundtrack Data")]
public class SoundtrackData : ScriptableObject
{
    [Header("Soundtrack Info")]
    [Tooltip("Unique ID for this track (e.g., 'CalmForest', 'TenseCaves'). Used for saving.")]
    public string soundtrackID;

    [Tooltip("Display name for the UI.")]
    public string displayName;

    [Tooltip("The actual music file.")]
    public AudioClip audioClip;

    [Header("UI")]
    [Tooltip("Icon to display in the selection menu.")]
    public Sprite icon;
}