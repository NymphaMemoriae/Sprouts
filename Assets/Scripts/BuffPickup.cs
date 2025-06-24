using UnityEngine;

public class BuffPickup : MonoBehaviour
{
    [Header("Buff Settings")]
    [SerializeField] private BuffData buffData;

    [Header("Visuals")]
    [SerializeField] private Animator animator;

    private bool collected = false;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collected || !collision.collider.CompareTag("PlantHead")) return;

        collected = true;
        Debug.Log("[BuffPickup] Collided with PlantHead");

        PlantController plant = collision.collider.GetComponentInParent<PlantController>();
        if (plant == null)
        {
            Debug.LogWarning("[BuffPickup] No PlantController found on PlantHead's parent.");
            return;
        }

        plant.ApplyBuff(buffData.buffType, buffData.duration);
        Debug.Log($"[BuffPickup] Buff applied: {buffData.buffType}");

        if (buffData.buffType == BuffType.ExtraLife && plant.plantLife != null)
        {
            plant.plantLife.AddLife(1);
        }
        // Handle coin-related buffs
        if (buffData.buffType == BuffType.AddCoins)
        {
            plant.AddDirectCoins(buffData.coinAmount);
        }
        else if (buffData.buffType == BuffType.DoubleCoins)
        {
            plant.ActivateDoubleCoins();
            // This buff is permanent for the run, so we can just disable the pickup object
            // after a visual effect or immediately.
        }

        if (animator != null)
        {
            animator.SetTrigger("BuffTrigger");
        }

        // Object remains active for pooling or visual finish
    }
}
