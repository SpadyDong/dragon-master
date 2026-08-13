// Figma2Unity preflight: checks package + File Key + PAT. Logs machine-readable
// [F2U-CHECK] lines. Run via execute_csharp_script (script_path). No user input here.
using System.Reflection;

var product = UnityEngine.Application.productName;

// 1) package installed?
var listReq = UnityEditor.PackageManager.Client.List(true, false);
while (!listReq.IsCompleted) { System.Threading.Thread.Sleep(50); }
bool pkg = false;
if (listReq.Result != null)
    foreach (var p in listReq.Result)
        if (p.name == "cn.tuanjie.figma2unity") { pkg = true; break; }

// 2) File Key set?
var fileKey = UnityEditor.EditorPrefs.GetString("Figma2Unity.FileKey", "");
bool hasKey = !string.IsNullOrEmpty(fileKey);

// 3) PAT set?
var patKey = "Figma2Unity." + product + ".PAT";
var pat = UnityEditor.EditorPrefs.GetString(patKey, "");
bool hasPat = !string.IsNullOrEmpty(pat);

UnityEngine.Debug.Log($"[F2U-CHECK] package={pkg}");
UnityEngine.Debug.Log($"[F2U-CHECK] fileKey={hasKey}");
UnityEngine.Debug.Log($"[F2U-CHECK] patKey={patKey}");
UnityEngine.Debug.Log($"[F2U-CHECK] pat={hasPat}");
UnityEngine.Debug.Log($"[F2U-CHECK] ready={(pkg && hasKey && hasPat)}");
