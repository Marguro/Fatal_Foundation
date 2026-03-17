# ระบบ Perk - คู่มือการตั้งค่า

## 📋 สารบัญของ Scripts ที่สร้าง

### 1. **PerkData.cs** (Scriptable Object)
- **ความหมาย**: ข้อมูลของ Perk แต่ละตัว
- **คุณสมบัติ**:
  - ชื่อ, คำอธิบาย, และรูปภาพของ Perk
  - ราคา Soul Orb
  - โบนัส stats (Health, Damage, Speed, Stamina)
  - ฟังก์ชัน `ApplyPerk()` สำหรับใช้ Perk

### 2. **SoulOrbCurrency.cs** (NetworkBehaviour)
- **ความหมาย**: ระบบจัดการสกุลเงิน Soul Orb ของผู้เล่น
- **ฟังก์ชันหลัก**:
  - `AddSoulOrbs(int amount)` - เพิ่ม Soul Orb
  - `RemoveSoulOrbs(int amount)` - ลบ Soul Orb (เมื่อซื้อ Perk)
  - `HasEnoughSoulOrbs(int amount)` - ตรวจสอบว่ามี Soul Orb พอหรือไม่
  - `OnSoulOrbsChanged` - Event ที่ไฟเมื่อจำนวน Soul Orb เปลี่ยน

### 3. **PerkSystem.cs** (NetworkBehaviour)
- **ความหมาย**: ระบบจัดการ Perk หลัก
- **ฟังก์ชันหลัก**:
  - `GetRandomPerkSelection(int count)` - สุ่มเลือก Perks เพื่อให้ผู้เล่นเลือก
  - `SelectPerk(PerkData perk, GameObject character)` - เลือก Perk และใช้มัน
  - `IsPerkSelected(PerkData perk)` - ตรวจสอบว่า Perk ได้รับการเลือกแล้วหรือไม่
  - `OnPerkSelected` - Event ที่ไฟเมื่อ Perk ถูกเลือก

### 4. **PerkUIManager.cs** (MonoBehaviour)
- **ความหมาย**: จัดการ UI Canvas สำหรับเลือก Perk
- **ฟังก์ชันหลัก**:
  - `OpenPerkMenu()` - เปิด Canvas พร้อมปุ่ม Perk แบบสุ่ม 3 ตัว
  - `ClosePerkMenu()` - ปิด Canvas
  - `SelectPerk(PerkData perk)` - เลือก Perk (Highlight)
  - `ConfirmPerkSelection()` - ยืนยันและใช้ Perk ที่เลือก
  - `UpdateSoulOrbDisplay(int amount)` - อัปเดตการแสดงผล Soul Orb

### 5. **PerkButtonUI.cs** (MonoBehaviour)
- **ความหมาย**: UI ปุ่มสำหรับแต่ละ Perk
- **คุณสมบัติ**:
  - แสดงรูปภาพ, ชื่อ, คำอธิบาย, และราคา Perk
  - `SetHighlight(bool)` - เปลี่ยน highlight เมื่อเลือก

### 6. **PerkInputHandler.cs** (MonoBehaviour)
- **ความหมาย**: จัดการ Input จากผู้เล่น (กด P หรือ Tap)
- **Supported Input**:
  - Keyboard: กด `P` (สามารถเปลี่ยนใน Inspector)
  - Touch: Tap ที่จอ

---

## 🎮 วิธีการตั้งค่า (Setup Guide)

### ขั้นตอนที่ 1: สร้าง Perks (Scriptable Objects)

1. ไปที่ `Assets/Resources/` (สร้าง folder นี้ถ้ายังไม่มี)
2. **คลิกขวา** → **Create** → **FatalFoundation/Perks/Perk**
3. ตั้งชื่อ เช่น "Perk_HealthBoost"
4. กรอกข้อมูล:
   - **Perk Name**: "Health Boost"
   - **Description**: "เพิ่มสุขภาพ +50"
   - **Soul Orb Cost**: 5
   - **Health Bonus**: 50

ทำซ้ำสำหรับ Perks อื่น ๆ ที่ต้องการ

### ขั้นตอนที่ 2: ตั้งค่า Scene

#### A. สร้าง Perk System Manager GameObject

1. สร้าง **Empty GameObject** ชื่อ "PerkManager"
2. เพิ่ม script **PerkSystem.cs** ให้กับมัน
3. ใน Inspector:
   - ใส่ Perks ที่สร้างไว้ในช่อง **All Available Perks**

#### B. สร้าง Soul Orb Currency GameObject

1. สร้าง **Empty GameObject** ชื่อ "SoulOrbCurrency"
2. เพิ่ม script **SoulOrbCurrency.cs**
3. ใน Inspector:
   - **Starting Soul Orbs**: ตั้งค่าจำนวน Soul Orb เริ่มต้น (เช่น 10)

**⚠️ สำคัญ**: ตั้ง **Tag** ของ Player GameObject เป็น "Player"

#### C. ตั้งค่า UI Canvas

1. สร้าง **Canvas** (หรือใช้ Canvas ที่มีอยู่)
2. สร้าง **Panel** ใหม่ชื่อ "PerkPanel"
3. เพิ่ม script **PerkUIManager.cs** ให้กับ Canvas
4. สร้าง **Button** ชื่อ "CloseButton" (เพื่อปิด Canvas)
5. สร้าง **TextMeshPro** ชื่อ "SoulOrbDisplay" สำหรับแสดงจำนวน Soul Orb

#### D. สร้าง Perk Button Prefab

1. สร้าง **Panel** ใหม่เป็น prefab สำหรับปุ่ม Perk:
   - ใจกว่าง **PerkButtonPrefab** ที่จะใช้ซ้ำ ๆ
   - ที่ภายในเพิ่มส่วนประกอบ:
     - **Image** สำหรับ Perk Icon
     - **TextMeshPro** สำหรับชื่อ Perk
     - **TextMeshPro** สำหรับคำอธิบาย
     - **TextMeshPro** สำหรับราคา
     - **Button** สำหรับเลือก

2. เพิ่ม script **PerkButtonUI.cs** ให้กับ Panel
3. ใน Inspector:
   - ลากไปใส่ UI elements ในช่องต่าง ๆ

4. ลากปุ่มนี้ไป `Assets/Prefabs/` (สร้าง folder นี้หากยังไม่มี)

#### E. เชื่อมต่อ UI ใน PerkUIManager

1. เลือก Canvas ที่มี PerkUIManager
2. ใน Inspector:
   - **Perk Canvas**: ลาก PerkPanel
   - **Close Button**: ลาก CloseButton
   - **Soul Orb Display Text**: ลาก SoulOrbDisplay
   - **Perk Button Prefab**: ลาก PerkButtonPrefab prefab
   - **Perk Button Container**: สร้าง Empty GameObject ใน PerkPanel ชื่อ "ButtonContainer" และลากมา

#### F. เพิ่ม Input Handler

1. สร้าง **Empty GameObject** ชื่อ "PerkInputHandler"
2. เพิ่ม script **PerkInputHandler.cs**
3. สามารถตั้ง **Perk Menu Key** (ค่าเริ่มต้นคือ P)

---

## 🎯 วิธีการใช้งาน (How to Use)

### เปิด Perk Menu
```
- **Keyboard**: กด `P` (หรือ key ที่ตั้งไว้)
- **Touch**: Tap ที่จอ
```

### ในเมนู Perk:
1. ปุ่มจำนวน **3 ปุ่ม** ปรากฏแบบสุ่ม
2. **คลิก** ปุ่ม Perk เพื่อ **Highlight** (จะเปลี่ยนสีเป็นสีเหลือง)
3. **คลิก "Confirm"** เพื่อยืนยันการเลือก:
   - ตรวจสอบว่ามี Soul Orb พอ
   - หากพอ: หักเงิน Soul Orb และใช้ Perk → ปิด Canvas
   - หากไม่พอ: แสดง Warning
4. **คลิก "Close"** เพื่อปิด Canvas โดยไม่เลือกอะไร

---

## 📊 Code Examples

### เพิ่ม Soul Orbs (เมื่อชนะศัตรู หรือหาของ)

```csharp
// ที่ Enemies.cs หรือ Reward System
SoulOrbCurrency.Instance.AddSoulOrbs(5);
```

### สร้าง Custom Perk

```csharp
public class CustomPerkData : PerkData
{
    public override void ApplyPerk(GameObject character)
    {
        base.ApplyPerk(character);
        
        // Custom logic
        Debug.Log("Custom Perk Applied!");
    }
}
```

### ตรวจสอบ Perk ที่เลือก

```csharp
var selectedPerks = PerkSystem.Instance.GetSelectedPerks();
foreach (var perkName in selectedPerks)
{
    Debug.Log("Player has: " + perkName);
}
```

---

## 🐛 Troubleshooting

| ปัญหา | วิธีแก้ |
|------|--------|
| Canvas ไม่ปรากฏ | ตรวจสอบว่า PerkUIManager เชื่อมต่อ Perk Canvas แล้ว |
| ปุ่มมีปัญหา | ทำให้ PerkButtonPrefab มี script PerkButtonUI |
| Soul Orb ไม่อัปเดต | ตรวจสอบว่า SoulOrbDisplay ถูกลากมาแล้ว |
| Input ไม่ทำงาน | ตรวจสอบ PerkInputHandler อยู่ในScene และ enabled |
| Player ไม่พบ | ตั้ง Tag ของ Player GameObject เป็น "Player" |

---

## 🔧 Advanced Customization

### เปลี่ยนจำนวนปุ่ม Perk

ใน `PerkUIManager.cs`:
```csharp
List<PerkData> perks = PerkSystem.Instance.GetRandomPerkSelection(5); // เปลี่ยน 3 เป็น 5
```

### เปลี่ยนสี Highlight

ใน PerkUIManager Inspector:
- **Highlight Color**: เลือกสีใหม่ (ค่าเริ่มต้นคือสีเหลือง)

### เปลี่ยน Key Input

ใน PerkInputHandler Inspector:
- **Perk Menu Key**: เลือก Key ใหม่ (ค่าเริ่มต้นคือ P)

---

## 📝 สรุป

ระบบ Perk นี้ออกแบบมา เพื่อให้:
✅ ผู้เล่นกดเปิด UI ได้ง่าย
✅ เลือก Perk พร้อม Highlight
✅ ใช้ Soul Orb เป็นสกุลเงิน
✅ ระบบ Networking ที่ดี (Netcode for GameObjects)
✅ สามารถขยายเพิ่มเติมได้ง่าย

Enjoy! 🎮

