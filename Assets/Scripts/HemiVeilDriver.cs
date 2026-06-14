using System.IO;
using System.Text;
using UnityEngine;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;

// MRTK 2.8 version of HemiVeilDriver
// requires MRTK Foundation (2.8) in the project, eye tracking enabled in the MRTK Input System
// profile, and the GazeInput capability ticked in Player publishing settings.


[DisallowMultipleComponent]
public class HemiVeilDriverMRTK : MonoBehaviour
{
    [Header("Reference (assign the HemiVeil material asset)")]
    public Material veilMaterial;

    [Header("Clinical parameters")]
    [Range(0f, 5f)]   public float macularSparingDeg = 1f;   // Schuett et al. ~1 degree default
    [Range(0.1f, 5f)] public float boundaryFeatherDeg = 1.5f;
    public bool  blindOnRight = true;                          // true = right HH (+1), false = left HH (-1)
    [Range(0f, 1f)] public float maxOpacity = 1f;
    public Color veilColor = new Color(0.5f, 0.5f, 0.5f, 1f);  // keep mid/bright: black is invisible on OST

    [Header("Fixes / toggles")]
    [Tooltip("Flip if the spared disc tracks vertically inverted relative to where you look.")]
    public bool flipGazeY = false;

    [Tooltip("Log one warning if eye tracking is on but the headset isn't calibrated for this user.")]
    public bool warnIfUncalibrated = true;

    [Header("Logging")]
    public bool logGaze = true;

    Camera cam;
    bool calibrationWarned = false;

    StringBuilder buf = new StringBuilder();
    float lastFlush;
    string path;

    int idGazeUV, idFovX, idFovY, idSpare, idEdge, idSide, idAlpha, idColor;

    void Start()
    {
        cam = Camera.main;

        idGazeUV = Shader.PropertyToID("_GazeUV");
        idFovX   = Shader.PropertyToID("_FovXDeg");
        idFovY   = Shader.PropertyToID("_FovYDeg");
        idSpare  = Shader.PropertyToID("_SparingDeg");
        idEdge   = Shader.PropertyToID("_EdgeDeg");
        idSide   = Shader.PropertyToID("_Side");
        idAlpha  = Shader.PropertyToID("_MaxAlpha");
        idColor  = Shader.PropertyToID("_VeilColor");

        path = Path.Combine(Application.persistentDataPath, "gaze_log.csv");
        if (logGaze) buf.AppendLine("t,gazeUVx,gazeUVy,valid,side,sparingDeg");
    }

    void Update()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null || veilMaterial == null) return;

        // read gaze from MRTK
        var gaze = CoreServices.InputSystem?.EyeGazeProvider;

        bool valid = gaze != null && gaze.IsEyeTrackingEnabledAndValid;
        Vector3 gazeOrigin = valid ? gaze.GazeOrigin    : cam.transform.position;
        Vector3 gazeDir    = valid ? gaze.GazeDirection : cam.transform.forward;

        if (warnIfUncalibrated && !calibrationWarned && gaze != null &&
            gaze.IsEyeTrackingEnabled && gaze.IsEyeCalibrationValid == false)
        {
            Debug.LogWarning("[HemiVeil] Eye tracking is enabled but this user is not calibrated. " +
                             "Run HoloLens eye calibration for accurate gaze-contingent simulation.");
            calibrationWarned = true;
        }

        //project gaze to viewport (0..1)
        Vector3 vp = cam.WorldToViewportPoint(gazeOrigin + gazeDir);
        float gx = Mathf.Clamp01(vp.x);
        float gy = Mathf.Clamp01(vp.y);
        if (flipGazeY) gy = 1f - gy;

        //camera FOV in degrees
        float vFov = cam.fieldOfView;
        float hFov = 2f * Mathf.Atan(Mathf.Tan(vFov * Mathf.Deg2Rad * 0.5f) * cam.aspect) * Mathf.Rad2Deg;

        //push to the material
        veilMaterial.SetVector(idGazeUV, new Vector4(gx, gy, 0f, 0f));
        veilMaterial.SetFloat (idFovX,  hFov);
        veilMaterial.SetFloat (idFovY,  vFov);
        veilMaterial.SetFloat (idSpare, macularSparingDeg);
        veilMaterial.SetFloat (idEdge,  boundaryFeatherDeg);
        veilMaterial.SetFloat (idSide,  blindOnRight ? 1f : -1f);
        veilMaterial.SetFloat (idAlpha, maxOpacity);
        veilMaterial.SetColor (idColor, veilColor);

        //buffered gaze log
        if (logGaze)
        {
            buf.Append(Time.time.ToString("F4")).Append(',')
               .Append(gx.ToString("F4")).Append(',')
               .Append(gy.ToString("F4")).Append(',')
               .Append(valid ? 1 : 0).Append(',')
               .Append(blindOnRight ? 1 : -1).Append(',')
               .Append(macularSparingDeg.ToString("F2")).Append('\n');

            if (Time.time - lastFlush > 2f)
            {
                File.AppendAllText(path, buf.ToString());
                buf.Clear();
                lastFlush = Time.time;
            }
        }
    }

    void OnDisable()
    {
        if (logGaze && buf.Length > 0)
        {
            File.AppendAllText(path, buf.ToString());
            buf.Clear();
        }
    }
}