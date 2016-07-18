using System.Collections;
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
            StartCoroutine(CaptureScreen(true));
        } else if (Input.GetKeyDown(KeyCode.G) && !cameraPanBlocked) {
            StartCoroutine(CaptureScreen(false));
        }
    }

    public IEnumerator CaptureScreen(bool UIon) {
        // Wait for screen rendering to complete
        yield return new WaitForEndOfFrame();

        // Take screenshot
        string filename = customPath + imageName + index + ".png";
        if (!UIon) {
            Application.CaptureScreenshot(customPath + imageName + index + ".png", resolution);
            Debug.Log(string.Format("Took screenshot to: {0} (UI elements off)", filename));
        } else {
            int resWidth = Screen.currentResolution.width * resolution;
            int resHeight = Screen.currentResolution.height * resolution;

            RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);
            cam.targetTexture = rt;
            Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
            cam.Render();
            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
            cam.targetTexture = null;
            RenderTexture.active = null; // JC: added to avoid errors
            Destroy(rt);
            byte[] bytes = screenShot.EncodeToPNG();
            System.IO.File.WriteAllBytes(filename, bytes);
            Debug.Log(string.Format("Took screenshot to: {0} (UI elements on)", filename));
        }
        index++;
    }

    void OnApplicationQuit() {
        PlayerPrefs.SetInt("ScreenshotIndex", (index));
    }
}