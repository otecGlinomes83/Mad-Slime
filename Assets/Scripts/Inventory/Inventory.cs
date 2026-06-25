using UnityEngine;
using YG;

public sealed class Inventory : MonoBehaviour
{
    public void IncreaseDefaultCount()
    {
        YG2.saves.DefaultCount++;
    }

    public void IncreaseQuotaCount()
    {
        YG2.saves.QuotaCount++;
    }
}