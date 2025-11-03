using UnityEngine;

public class UIFollowTarget : MonoBehaviour
{
    [SerializeField] private Transform worldTarget; // Player transform
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private Canvas canvas;         // The screen-space canvas
    [SerializeField] private Camera cam;            // The UI camera (usually Camera.main)

    private RectTransform rt;

    private void Reset()
    {
        rt = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        cam = Camera.main;
    }

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (cam == null) cam = Camera.main;
    }

    private void LateUpdate()
    {
        if (worldTarget == null || rt == null || cam == null) return;

        Vector3 screenPos = cam.WorldToScreenPoint(worldTarget.position + worldOffset);

        // If target behind camera, optionally hide
        bool off = screenPos.z < 0f;
        if (off)
        {
            rt.gameObject.SetActive(false);
            return;
        }
        else if (!rt.gameObject.activeSelf)
        {
            rt.gameObject.SetActive(true);
        }

        // Screen Space Overlay: set position directly
        // Screen Space Camera: also works to set position; Canvas handles it
        rt.position = screenPos;
    }
}
