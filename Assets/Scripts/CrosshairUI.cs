using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class CrosshairUI : MonoBehaviour
{
    [Header("Crosshair Settings")]
    [Tooltip("Color of the crosshair.")]
    public Color crosshairColor = Color.white;
    
    [Tooltip("Size of the crosshair (width and height).")]
    public float size = 10f;
    
    private void Start()
    {
        // ถ้าถูกแปะลงในผู้เล่นโดยตรง (ใน Prefab)
        NetworkObject netObj = GetComponentInParent<NetworkObject>();
        if (netObj != null)
        {
            // ตรวจสอบว่าถ้ายังไม่ Spawn หรือไม่ใช่ผู้เล่นตัวเอง ให้หยุดการทำงาน (กรณีนี้จะไม่เกิดถ้าแปะไว้ที่ Scene ธรรมดา)
            if (!netObj.IsSpawned || !netObj.IsOwner)
                return;
        }

        CreateCrosshair();
    }

    private void CreateCrosshair()
    {
        // === สร้าง Canvas ชิ้นใหม่แยกออกมาต่างหากเลย เพื่อให้มั่นใจว่าจะไม่ไปสิงกับ Canvas ที่ถูกปิดใช้งานอยู่ ===
        GameObject canvasObj = new GameObject("CrosshairCanvas_Independent");
        Canvas targetCanvas = canvasObj.AddComponent<Canvas>();
        targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        targetCanvas.sortingOrder = 99; // ให้อยู่บนสุดเสมอ
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject crosshairObj = new GameObject("Crosshair_Dot");
        crosshairObj.transform.SetParent(targetCanvas.transform, false);

        Image crosshairImage = crosshairObj.AddComponent<Image>();
        crosshairImage.color = crosshairColor;
        
        RectTransform rt = crosshairImage.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(size, size);
    }
}
