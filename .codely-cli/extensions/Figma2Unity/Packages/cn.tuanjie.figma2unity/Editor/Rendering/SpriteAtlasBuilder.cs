using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace Figma2Unity.Editor.Rendering
{
    /// <summary>
    /// v2 Track A — builds a single <see cref="SpriteAtlas"/> from every sprite the import
    /// pipeline emitted (IMAGE Paint downloads + Figmage bakes + VectorPngFallback).
    /// Atlas is a first-class pipeline output (Design.md §6.4) — without it the
    /// per-sprite textures kill draw-call batching.
    ///
    /// Idempotent: re-running on the same project reuses the existing .spriteatlas and
    /// only updates its packable list. Skip if no sprites or atlas disabled in config.
    /// </summary>
    public class SpriteAtlasBuilder
    {
        public virtual SpriteAtlas Build(F2UContext ctx, string atlasPath)
        {
            if (ctx == null) return null;
            if (ctx.Config != null && !ctx.Config.EnableSpriteAtlas) return null;
            if (string.IsNullOrEmpty(atlasPath)) return null;
            if (ctx.NodeSpritePathMap == null || ctx.NodeSpritePathMap.Count == 0) return null;

            // Dedup & filter — multiple nodes can share one imageRef / Sprite.
            var packables = new List<Sprite>();
            var seenAssets = new HashSet<string>();
            foreach (var path in ctx.NodeSpritePathMap.Values)
            {
                if (string.IsNullOrEmpty(path)) continue;
                if (!seenAssets.Add(path)) continue;
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null) packables.Add(sprite);
            }
            if (packables.Count == 0) return null;

            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            if (atlas == null)
            {
                atlas = new SpriteAtlas();
                AssetDatabase.CreateAsset(atlas, atlasPath);
            }

            // Replace the packable set wholesale — running the pipeline twice over a Figma
            // change should converge on the current sprite list, not accumulate stragglers.
            var existing = atlas.GetPackables();
            if (existing != null && existing.Length > 0)
                atlas.Remove(existing);
            atlas.Add(packables.ToArray());

            atlas.SetIncludeInBuild(true);

            var packing = atlas.GetPackingSettings();
            packing.padding = 4;
            packing.enableTightPacking = false; // UGUI needs full-rect for 9-slice corners
            atlas.SetPackingSettings(packing);

            var platform = atlas.GetPlatformSettings("DefaultTexturePlatform");
            platform.overridden = true;
            platform.textureCompression = TextureImporterCompression.CompressedHQ;
            atlas.SetPlatformSettings(platform);

            EditorUtility.SetDirty(atlas);
            return atlas;
        }
    }
}
