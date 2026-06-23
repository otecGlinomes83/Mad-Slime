using UnityEngine;

public sealed class Inventory : MonoBehaviour
{
    public int QuotaCount { get; private set; }
    public int DefaultCount { get; private set; }

    public void IncreaseDefaultCount()
    {
        DefaultCount++;
    }

    public void IncreaseQuotaCount()
    {
        QuotaCount++;
    }
}