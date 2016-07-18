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
    
    public int resolution = 3; // 1= default, 2= 2x default, etc.
    public string imageName = "Screenshot_";
    public string customPath = "C:/BART/UNIF/Masterthesis/UnityScreenshots/"; // leave blank for project file location
    public bool resetIndex = false;

    private int index = 0;

    void Start() {
        if (cam == null) cam = Camera.main;
        targetOrtho = cam.orthographicSize;

        if (resetIndex) PlayerPrefs.SetInt("ScreenshotIndex", 0);
        if (customPath != "") {
            if (!System.IO.Directory.Exists(customPath)) {
                System.IO.Directory.CreateDirectory(customPath);
            }
        }
        index = PlayerPrefs.GetInt("ScreenshotIndex") != 0 ? PlayerPrefs.GetInt("ScreenshotIndex") : 1;
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

    void LateUpdate() {
        if (Input.GetKeyDown(KeyCode.F) && !cameraPanBlocked) {
            Application.CaptureScreenshot(customPath + imageName + index + ".png", resolution);
            index++;
            Debug.LogWarning("Screenshot saved: " + customPath + " --- " + imageName + index);
        }
    }

    void OnApplicationQuit() {
        PlayerPrefs.SetInt("ScreenshotIndex", (index));
    }
}