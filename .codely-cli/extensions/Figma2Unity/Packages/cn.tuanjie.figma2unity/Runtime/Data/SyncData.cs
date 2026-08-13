using System;
using System.Collections.Generic;

namespace Figma2Unity
{
    /// <summary>Persisted per-node sync metadata, attached via SyncHelper.</summary>
    [Serializable]
    public class SyncData
    {
        public string FigmaId;
        public string ProjectId;
        public List<FcuTag> Tags = new List<FcuTag>();
        public string FolderName;
        public string FileName;
        public string FigmaName;
        public string HierarchyPath;
        public string SpritePath;
        public FObjectHashData HashData;
    }
}
