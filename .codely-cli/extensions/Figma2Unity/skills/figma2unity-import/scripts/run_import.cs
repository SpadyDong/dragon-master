// Trigger the Figma2Unity import WITHOUT showing the Main Window panel.
// Creates a hidden F2UMainWindow instance (never Show()/GetWindow), seeds its private
// credential fields from EditorPrefs, then invokes the private ImportAsync() method that
// the "Import" button is wired to. Run via execute_csharp_script (script_path).
using System.Reflection;

const BindingFlags IF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
var t = typeof(Figma2Unity.Editor.F2UMainWindow);
var product = UnityEngine.Application.productName;

// Hidden instance: CreateInstance runs the ctor (field initializers -> Full mode, cache OFF)
// and OnEnable (loads prefs). We do NOT call Open()/GetWindow(), so no window appears.
var win = UnityEngine.ScriptableObject.CreateInstance(t) as UnityEditor.EditorWindow;
if (win == null) { UnityEngine.Debug.LogError("[F2U-IMPORT] could not create window instance"); return; }
win.hideFlags = UnityEngine.HideFlags.HideAndDontSave;

// Belt-and-suspenders: seed credentials directly from EditorPrefs so import does not
// depend on OnEnable having run. Field names match F2UMainWindow (_fileKey, _token).
var fileKey = UnityEditor.EditorPrefs.GetString("Figma2Unity.FileKey", "");
var pat = UnityEditor.EditorPrefs.GetString("Figma2Unity." + product + ".PAT", "");
t.GetField("_fileKey", IF)?.SetValue(win, fileKey);
t.GetField("_token", IF)?.SetValue(win, pat);

var mi = t.GetMethod("ImportAsync", IF);
if (mi == null)
{
    UnityEngine.Debug.LogError("[F2U-IMPORT] ImportAsync not found");
    UnityEngine.Object.DestroyImmediate(win);
    return;
}

// Fire the async import. Do NOT destroy the instance here: ImportAsync is async and needs
// the object alive. It was never shown, so it stays hidden.
mi.Invoke(win, null);
UnityEngine.Debug.Log("[F2U-IMPORT] triggered (hidden window)");
