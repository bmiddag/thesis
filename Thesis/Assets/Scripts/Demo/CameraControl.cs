using UnityEngine;

public class CameraControl : MonoBehaviour {

    public float zoomSpeed = 1000;
    public float targetOrtho;
    public float minOrtho = 360.0f;
    public float maxOrtho = 1440.0f;

    public Camera cam;
    Vector3 panCenter;
    bool panning = false;
    public bool cameraPanBlocked = false;

    void Start() {
        if (cam == null) cam = Camera.main;
        targetOrtho = cam.orthographicSize;
    }

    void Update() {
        // Pan
        if (Input.GetMouseButton(0) && !cameraPanBlocked) {
            if (!panning) {
                panCenter = cam.ScreenToWorldPoint(Input.mousePosition);
                panning = true;
            } else {
                cam.transform.position = cam.transform.position - cam.ScreenToWorldPoint(Input.mousePosition) + panCenter;
                panCenter = cam.ScreenToWorldPoint(Input.mousePosition);
            }
        } else {
            if (panning) panning = false;
        }

        // Zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0.0f) {
            targetOrtho -= scroll * zoomSpeed;
            targetOrtho = Mathf.Clamp(targetOrtho, minOrtho, maxOrtho);
        }
        cam.orthographicSize = Mathf.MoveTowards(cam.orthographicSize, targetOrtho, Mathf.Abs((cam.orthographicSize-targetOrtho) * Time.deltaTime*7));
    }
}