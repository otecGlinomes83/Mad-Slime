using System;
using Skins;
using UnityEngine;

namespace MadSlime.Tests
{
    public class SkinInventoryValidation
    {
        public static void Validate()
        {
            SkinStorageFixture storage = new SkinStorageFixture();
            storage.OpenSkin("equipped");
            storage.SelectSkin("equipped");
            SkinInventory inventory = new SkinInventory(storage);

            if (inventory.Select("locked") == true || inventory.SelectedSkinId != "equipped")
            {
                throw new InvalidOperationException("Locked skin selection changed equipped skin.");
            }

            if (inventory.Grant("locked") == false || inventory.Grant("locked") == true)
            {
                throw new InvalidOperationException("Skin grant must be idempotent.");
            }

            if (storage.OpenCalls != 2 || inventory.Select("locked") == false || inventory.SelectedSkinId != "locked")
            {
                throw new InvalidOperationException("Granted skin was not selected.");
            }

            int selectionCalls = storage.SelectionCalls;
            inventory.Select("locked");

            if (storage.SelectionCalls != selectionCalls)
            {
                throw new InvalidOperationException("Repeated selection wrote storage again.");
            }

            Debug.Log("SKIN INVENTORY PASS: locked selection, idempotent grant, selection persistence");
        }
    }
}
