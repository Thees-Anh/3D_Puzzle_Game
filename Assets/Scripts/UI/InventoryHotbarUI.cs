using PuzzleRoom.Core;
using PuzzleRoom.Player;
using UnityEngine;

namespace PuzzleRoom.UI
{
    /// <summary>Minimal three-slot hotbar and center aiming dot.</summary>
    public sealed class InventoryHotbarUI : MonoBehaviour
    {
        private PlayerInventory inventory;
        private Texture2D whiteTexture;

        private void Awake()
        {
            inventory = GetComponent<PlayerInventory>();
            whiteTexture = Texture2D.whiteTexture;
        }

        private void OnGUI()
        {
            DrawHotbar();
            GUI.color = Color.white;
        }

        private void DrawHotbar()
        {
            const float slotSize = 64f;
            const float gap = 8f;
            float totalWidth = slotSize * 3f + gap * 2f;
            float startX = Screen.width * 0.5f - totalWidth * 0.5f;
            float y = Screen.height - 88f;

            for (int index = 0; index < 3; index++)
            {
                Rect slot = new Rect(startX + index * (slotSize + gap), y, slotSize, slotSize);
                bool selected = inventory != null && inventory.SelectedSlotIndex == index;
                GUI.color = selected ? new Color(0.95f, 0.66f, 0.18f, 0.95f) : new Color(0.16f, 0.18f, 0.19f, 0.88f);
                GUI.DrawTexture(slot, whiteTexture);
                GUI.color = new Color(0.035f, 0.04f, 0.045f, 0.96f);
                GUI.DrawTexture(new Rect(slot.x + 3f, slot.y + 3f, slot.width - 6f, slot.height - 6f), whiteTexture);

                GUI.color = Color.white;
                GUI.Label(new Rect(slot.x + 5f, slot.y + 3f, 18f, 18f), (index + 1).ToString());
                ItemId item = inventory != null ? inventory.GetSlotItem(index) : (ItemId)index;
                if (inventory != null && inventory.HasItem(item)) DrawItemIcon(item, slot);
                if (inventory != null && GUI.Button(slot, GUIContent.none, GUIStyle.none)) inventory.SelectSlot(index);
            }
        }

        private void DrawItemIcon(ItemId item, Rect slot)
        {
            if (item == ItemId.UVLight)
            {
                GUI.color = new Color(0.95f, 0.72f, 0.12f);
                GUI.DrawTexture(new Rect(slot.x + 21f, slot.y + 29f, 30f, 10f), whiteTexture);
                GUI.DrawTexture(new Rect(slot.x + 43f, slot.y + 24f, 11f, 20f), whiteTexture);
            }
            else if (item == ItemId.Key)
            {
                GUI.color = new Color(0.88f, 0.58f, 0.14f);
                GUI.DrawTexture(new Rect(slot.x + 20f, slot.y + 30f, 34f, 7f), whiteTexture);
                GUI.DrawTexture(new Rect(slot.x + 18f, slot.y + 24f, 13f, 19f), whiteTexture);
                GUI.DrawTexture(new Rect(slot.x + 45f, slot.y + 35f, 6f, 9f), whiteTexture);
            }
            else
            {
                GUI.color = new Color(0.28f, 0.76f, 0.82f);
                GUI.DrawTexture(new Rect(slot.x + 27f, slot.y + 20f, 12f, 32f), whiteTexture);
                GUI.color = new Color(0.72f, 0.43f, 0.16f);
                GUI.DrawTexture(new Rect(slot.x + 24f, slot.y + 17f, 18f, 7f), whiteTexture);
                GUI.DrawTexture(new Rect(slot.x + 24f, slot.y + 48f, 18f, 7f), whiteTexture);
            }
            GUI.color = Color.white;
        }
    }
}
