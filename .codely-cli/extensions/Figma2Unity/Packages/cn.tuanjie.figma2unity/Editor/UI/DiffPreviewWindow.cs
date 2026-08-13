using System.Collections.Generic;
using Figma2Unity.Editor.Sync;
using UnityEditor;
using UnityEngine;

namespace Figma2Unity.Editor
{
    /// <summary>
    /// v2 Track C — visualizes the result of an Incremental import. Four collapsible
    /// sections (Added / Modified / Deleted / Unchanged) with node Name + Id rows. The
    /// window is purely informational; SyncService has already applied the diff to the
    /// scene before this window opens.
    /// </summary>
    public class DiffPreviewWindow : EditorWindow
    {
        private DiffResult _diff;
        private Vector2 _scroll;
        private bool _showAdded = true;
        private bool _showModified = true;
        private bool _showDeleted = true;
        private bool _showUnchanged = false;

        public static void Show(DiffResult diff)
        {
            var win = GetWindow<DiffPreviewWindow>("F2U Diff Preview");
            win.minSize = new Vector2(420, 320);
            win._diff = diff;
            win.Show();
            win.Repaint();
        }

        private void OnGUI()
        {
            if (_diff == null)
            {
                EditorGUILayout.HelpBox("No diff available. Run an Incremental import first.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Incremental Import Diff", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"{_diff.Added.Count} added  ·  {_diff.Modified.Count} modified  ·  "
                + $"{_diff.Deleted.Count} deleted  ·  {_diff.Unchanged.Count} unchanged");
            EditorGUILayout.Space();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawFObjectGroup("Added",    _diff.Added,     ref _showAdded);
            DrawFObjectGroup("Modified", _diff.Modified,  ref _showModified);
            DrawSyncGroup   ("Deleted",  _diff.Deleted,   ref _showDeleted);
            DrawFObjectGroup("Unchanged",_diff.Unchanged, ref _showUnchanged);
            EditorGUILayout.EndScrollView();
        }

        private static void DrawFObjectGroup(string title, List<FObject> nodes, ref bool expanded)
        {
            expanded = EditorGUILayout.Foldout(expanded, $"{title} ({nodes.Count})", true);
            if (!expanded) return;
            EditorGUI.indentLevel++;
            foreach (var n in nodes)
            {
                if (n == null) continue;
                EditorGUILayout.LabelField($"{n.Name}  ({n.Id})");
            }
            EditorGUI.indentLevel--;
        }

        private static void DrawSyncGroup(string title, List<SyncHelper> helpers, ref bool expanded)
        {
            expanded = EditorGUILayout.Foldout(expanded, $"{title} ({helpers.Count})", true);
            if (!expanded) return;
            EditorGUI.indentLevel++;
            foreach (var h in helpers)
            {
                if (h == null || h.Data == null) continue;
                EditorGUILayout.LabelField($"{h.Data.FigmaName}  ({h.Data.FigmaId})");
            }
            EditorGUI.indentLevel--;
        }
    }
}
