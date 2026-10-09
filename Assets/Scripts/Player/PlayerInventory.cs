using System;
using System.Collections.Generic;
using PuzzleRoom.Core;
using UnityEngine;
using PuzzleRoom.UI;

namespace PuzzleRoom.Player
{
    /// <summary>
    /// Lightweight runtime inventory for unique progression items.
    /// </summary>
    public sealed class PlayerInventory : MonoBehaviour
    {
        private readonly HashSet<ItemId> ownedItems = new HashSet<ItemId>();

#if UNITY_EDITOR
        [Header("Editor Testing")]
        [SerializeField]
        [Tooltip("Editor-only shortcut for testing the Fuse Box without replaying earlier puzzles.")]
        private bool debugStartWithFuse;
#endif

        public event Action<ItemId> ItemAdded;
        public event Action<ItemId> ItemRemoved;
        public event Action<int> SelectedSlotChanged;

        private static readonly ItemId[] SlotItems =
        {
            ItemId.UVLight,
            ItemId.Key,
            ItemId.Fuse
        };

        public int SelectedSlotIndex { get; private set; }

        public IReadOnlyCollection<ItemId> OwnedItems => ownedItems;

        private void Awake()
        {
            if (GetComponent<InventoryHotbarUI>() == null)
            {
                gameObject.AddComponent<InventoryHotbarUI>();
            }
#if UNITY_EDITOR
            if (debugStartWithFuse)
            {
                TryAddItem(ItemId.Fuse);
            }
#endif
        }

        public ItemId GetSlotItem(int slotIndex)
        {
            return SlotItems[Mathf.Clamp(slotIndex, 0, SlotItems.Length - 1)];
        }

        public bool HasSelectedItem(ItemId itemId)
        {
            return GetSlotItem(SelectedSlotIndex) == itemId && HasItem(itemId);
        }

        public void SelectSlot(int slotIndex)
        {
            int clamped = Mathf.Clamp(slotIndex, 0, SlotItems.Length - 1);
            if (SelectedSlotIndex == clamped) return;
            SelectedSlotIndex = clamped;
            SelectedSlotChanged?.Invoke(SelectedSlotIndex);
        }

        public bool HasItem(ItemId itemId)
        {
            return ownedItems.Contains(itemId);
        }

        public bool TryAddItem(ItemId itemId)
        {
            if (!ownedItems.Add(itemId))
            {
                return false;
            }

            ItemAdded?.Invoke(itemId);
            SelectSlot(Array.IndexOf(SlotItems, itemId));
            return true;
        }

        public bool TryRemoveItem(ItemId itemId)
        {
            if (!ownedItems.Remove(itemId))
            {
                return false;
            }

            ItemRemoved?.Invoke(itemId);
            return true;
        }

        public void RestoreItems(ItemId[] items, int selectedSlot)
        {
            ownedItems.Clear();
            if (items != null)
                foreach (ItemId item in items) ownedItems.Add(item);
            SelectedSlotIndex = Mathf.Clamp(selectedSlot, 0, SlotItems.Length - 1);
            foreach (ItemId item in SlotItems)
                if (ownedItems.Contains(item)) ItemAdded?.Invoke(item);
            SelectedSlotChanged?.Invoke(SelectedSlotIndex);
        }

#if UNITY_EDITOR
        [ContextMenu("Debug/Give Fuse Now")]
        private void DebugGiveFuseNow()
        {
            if (Application.isPlaying)
            {
                TryAddItem(ItemId.Fuse);
            }
        }
#endif
    }
}
