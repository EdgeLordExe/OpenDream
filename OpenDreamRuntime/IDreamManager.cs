using System;
using System.Collections.Generic;
using OpenDreamRuntime.Objects;
using Robust.Server.Player;

namespace OpenDreamRuntime {
    public interface IDreamManager {
        public DreamObjectTree ObjectTree { get; }
        public DreamValue WorldInstance { get; }
        public int DMExceptionCount { get; set; }

        public List<DreamValue> Globals { get; set; }
        public DreamList WorldContentsList { get; set; }
        public Dictionary<DreamValue, DreamList> AreaContents { get; set; }
        public Dictionary<DreamValue, int> ReferenceIDs { get; set; }
        List<DreamValue> Mobs { get; set; }
        public Random Random { get; set; }

        public void Initialize();
        public void Shutdown();
        public IPlayerSession GetSessionFromClient(DreamValue client);
        DreamConnection GetConnectionFromClient(DreamValue client);
        public DreamValue GetClientFromMob(DreamValue mob);
        DreamConnection GetConnectionFromMob(DreamValue mob);
        DreamConnection GetConnectionBySession(IPlayerSession session);
        void Update();

        IEnumerable<DreamConnection> Connections { get; }
    }
}
