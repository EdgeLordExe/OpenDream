using System.Collections.Generic;
using OpenDreamRuntime.Objects;
using OpenDreamShared.Dream;
using OpenDreamShared.Json;
using Robust.Shared.Maths;

namespace OpenDreamRuntime {
    interface IDreamMapManager {
        public Vector2i Size { get; }
        public int Levels { get; }

        public void Initialize();
        public void LoadMaps(List<DreamMapJson> maps);
        public void SetTurf(int x, int y, int z, DreamValue turf, bool replace = true);
        public void SetArea(int x, int y, int z, DreamValue area);
        public DreamValue GetTurf(int x, int y, int z);
        public DreamValue GetArea(DreamPath type);
        public DreamValue GetAreaAt(int x, int y, int z);
        public void SetZLevels(int levels);
    }
}
