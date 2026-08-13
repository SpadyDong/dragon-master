// Persist Figma2Unity File Key and/or PAT into EditorPrefs.
// Edit the two placeholders below before running via execute_csharp_script.
// Leave a value as the literal "__SKIP__" to keep the existing stored value.
var product = UnityEngine.Application.productName;

string fileKey = "__FILE_KEY__"; // raw key or full Figma URL, or "__SKIP__"
string pat      = "__PAT__";      // Figma Personal Access Token, or "__SKIP__"

if (fileKey != "__SKIP__")
    UnityEditor.EditorPrefs.SetString("Figma2Unity.FileKey", fileKey ?? "");
if (pat != "__SKIP__")
    UnityEditor.EditorPrefs.SetString("Figma2Unity." + product + ".PAT", pat ?? "");

var fkNow  = UnityEditor.EditorPrefs.GetString("Figma2Unity.FileKey", "");
var patNow = UnityEditor.EditorPrefs.GetString("Figma2Unity." + product + ".PAT", "");
UnityEngine.Debug.Log($"[F2U-SET] fileKey={!string.IsNullOrEmpty(fkNow)} pat={!string.IsNullOrEmpty(patNow)}");
