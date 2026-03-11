# Lethal Company-style Inventory System Prompt for GitHub Copilot

---

## 🎯 Objective
สร้างระบบ Inventory แบบเกม Lethal Company ใน Unity ที่มี 4 ช่องเก็บของ, ระบบน้ำหนัก (Weight), การสลับไอเทมในมือ (Visuals), และ UI ที่ค่อยๆ จางหายไป (Auto-hide)

---

## 📝 Master Prompt (Copy & Paste)

> "Create a complete, modular **Lethal Company-style Inventory System** in C# for Unity. The system should consist of the following components:
>
> 1.  **ItemData (ScriptableObject):** >     * Create a ScriptableObject to store item properties: `string itemName`, `Sprite itemIcon`, `GameObject worldPrefab` (for dropping), `GameObject handPrefab` (for holding), `float weight`, `int scrapValue`, and `bool isTwoHanded`.
>
> 2.  **PlayerInventory (Core Logic):**
      >     * Use a fixed array of `ItemData` with 4 slots.
>     * Implement a `currentSlotIndex` (0-3) controlled by the **Mouse Scroll Wheel**.
>     * Reference a `Transform handAnchor` to instantiate/destroy the `handPrefab` of the current item when switching slots.
>     * Create `PickUpItem(ItemData item)`: Adds item to the first empty slot.
>     * Create `DropItem()`: Spawns the `worldPrefab` at the player's feet and clears the active slot.
>     * Calculate `TotalWeight`: A property that sums the weight of all items in the inventory.
>
> 3.  **InventoryUI (Visuals):**
      >     * Manage 4 UI Slot Images.
>     * Highlight the active slot (e.g., change color or scale).
>     * Implement a **Fade-out Coroutine**: Show UI only when scrolling or picking up items, then fade it out (CanvasGroup.alpha) after 3 seconds of inactivity.
>
> 4.  **Integration & Movement:**
      >     * Logic: If the current item `isTwoHanded`, prevent the player from switching to other slots until dropped.
>     * Speed Penalty: Provide a formula where `finalSpeed = baseSpeed - (TotalWeight * weightMultiplier)`.
>
> Please use clean C# code, follow Unity best practices, and include comments for each method."

---

## 🛠️ โครงสร้างไฟล์ที่จะได้รับ (File Structure)
เมื่อ Copilot เขียนเสร็จ คุณควรจะได้ไฟล์หลักๆ ดังนี้:
1. `ItemData.cs` (ScriptableObject)
2. `PlayerInventory.cs` (MonoBehaviour - ติดที่ตัว Player)
3. `InventoryUI.cs` (MonoBehaviour - ติดที่ Canvas)
4. `InteractionSystem.cs` (Optional - สำหรับกด 'E' เพื่อเก็บของ)

---
