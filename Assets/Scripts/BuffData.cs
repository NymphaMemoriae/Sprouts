using UnityEngine;

[CreateAssetMenu(menuName = "Plant Game/Buff Data")]
public class BuffData : ScriptableObject
{
    public BuffType buffType;
    public float duration = 0f; // 0 means instant, like ExtraLife
    public bool isStackable = false;
}
