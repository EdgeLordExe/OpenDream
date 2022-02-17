using System;
using System.Collections.Generic;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;
using OpenDreamShared.Json;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace OpenDreamRuntime {
    class DreamMapManager : IDreamMapManager {
        public struct Cell {
            public DreamValue Turf;
            public DreamValue Area;
        };

        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IDreamManager _dreamManager = default!;
        [Dependency] private readonly IAtomManager _atomManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public Vector2i Size { get; private set; }
        public int Levels { get => _levels.Count; }

        private List<Cell[,]> _levels = new();
        private Dictionary<DreamPath, DreamValue> _areas = new();
        private DreamPath _defaultArea, _defaultTurf;

        public void Initialize() {
            _mapManager.CreateNewMapEntity(MapId.Nullspace);

            DreamObjectDefinition worldDefinition = _dreamManager.ObjectTree.GetObjectDefinition(DreamPath.World);
            _defaultArea = worldDefinition.Variables["area"].GetValueAsPath();
            _defaultTurf = worldDefinition.Variables["turf"].GetValueAsPath();
        }

        public void LoadMaps(List<DreamMapJson> maps) {
            if (maps.Count == 0) throw new ArgumentException("No maps were given");
            else if (maps.Count > 1) throw new NotImplementedException("Loading more than one map is not implemented");
            DreamMapJson map = maps[0];

            Size = new Vector2i(map.MaxX, map.MaxY);
            SetZLevels(map.MaxZ);

            foreach (MapBlockJson block in map.Blocks) {
                LoadMapBlock(block, map.CellDefinitions);
            }
        }

        public void SetTurf(int x, int y, int z, DreamValue turf, bool replace = true) {
            if (!IsValidCoordinate(x, y, z)) throw new ArgumentException("Invalid coordinates");

            _levels[z - 1][x - 1, y - 1].Turf = turf;

            EntityUid entity = _atomManager.GetAtomEntity(turf.GetValueAsDreamObject());
            if (!_entityManager.TryGetComponent<TransformComponent>(entity, out var transform))
                return;

            transform.AttachParent(_mapManager.GetMapEntityId(new MapId(z)));
            transform.LocalPosition = new Vector2(x, y);

            if (replace) {
                //Every reference to the old turf becomes the new turf
                //Do this by turning the old turf object into the new one
                DreamObject existingTurf = GetTurf(x, y, z).GetValueAsDreamObject();
                existingTurf.CopyFrom(turf.GetValueAsDreamObject());
            }
        }

        public void SetArea(int x, int y, int z, DreamValue area) {
            if (!IsValidCoordinate(x, y, z)) throw new ArgumentException("Invalid coordinates");
            DreamObject cachedArea = area.GetValueAsDreamObject();
            if (cachedArea.GetVariable("x").GetValueAsInteger() > x) cachedArea.SetVariable("x", new DreamValue(x));
            if (cachedArea.GetVariable("y").GetValueAsInteger() > y) cachedArea.SetVariable("y", new DreamValue(y));

            _levels[z - 1][x - 1, y - 1].Area = area;
        }

        public DreamValue GetTurf(int x, int y, int z) {
            if (!IsValidCoordinate(x, y, z)) return DreamValue.Null;

            return _levels[z - 1][x - 1, y - 1].Turf;
        }

        //Returns an area loaded by a DMM
        //Does not include areas created by DM code
        public DreamValue GetArea(DreamPath type) {
            if (!_areas.TryGetValue(type, out DreamValue area)) {
                area = _dreamManager.ObjectTree.CreateObject(type);
                area.GetValueAsDreamObject().InitSpawn(new(null));
                _areas.Add(type, area);
            }

            return area;
        }

        public DreamValue GetAreaAt(int x, int y, int z) {
            if (!IsValidCoordinate(x, y, z)) throw new ArgumentException("Invalid coordinates");

            return _levels[z - 1][x - 1, y - 1].Area;
        }

        public void SetZLevels(int levels) {
            if (levels > Levels) {
                for (int z = Levels + 1; z <= levels; z++) {
                    _levels.Add(new Cell[Size.X, Size.Y]);
                    _mapManager.CreateMap(new MapId(z));

                    for (int x = 1; x <= Size.X; x++) {
                        for (int y = 1; y <= Size.Y; y++) {
                            DreamValue turf = _dreamManager.ObjectTree.CreateObject(_defaultTurf);

                            turf.GetValueAsDreamObject().InitSpawn(new(null));
                            SetTurf(x, y, z, turf, replace: false);
                            SetArea(x, y, z, GetArea(_defaultArea));
                        }
                    }
                }
            } else if (levels < Levels) {
                _levels.RemoveRange(levels, Levels - levels);
                for (int z = Levels; z > levels; z--) {
                    _mapManager.DeleteMap(new MapId(z));
                }
            }
        }

        private bool IsValidCoordinate(int x, int y, int z) {
            return (x <= Size.X && y <= Size.Y && z <= Levels) && (x >= 1 && y >= 1 && z >= 1);
        }

        private void LoadMapBlock(MapBlockJson block, Dictionary<string, CellDefinitionJson> cellDefinitions) {
            int blockX = 1;
            int blockY = 1;

            foreach (string cell in block.Cells) {
                CellDefinitionJson cellDefinition = cellDefinitions[cell];
                DreamPath areaType = cellDefinition.Area != null ? _dreamManager.ObjectTree.Types[cellDefinition.Area.Type].Path : _defaultArea;
                DreamValue area = GetArea(areaType);

                int x = block.X + blockX - 1;
                int y = block.Y + block.Height - blockY;

                DreamValue turf;
                if (cellDefinition.Turf != null) {
                    turf = CreateMapObject(cellDefinition.Turf);
                } else {
                    turf = _dreamManager.ObjectTree.CreateObject(_defaultTurf);
                }
                
                SetTurf(x, y, block.Z, turf);
                SetArea(x, y, block.Z, area);
                turf.GetValueAsDreamObject().InitSpawn(new DreamProcArguments(null));
                    

                foreach (MapObjectJson mapObject in cellDefinition.Objects) {
                    var obj = CreateMapObject(mapObject);
                    obj.GetValueAsDreamObject().InitSpawn(new DreamProcArguments(new() { new DreamValue(turf) }));
                }

                blockX++;
                if (blockX > block.Width) {
                    blockX = 1;
                    blockY++;
                }
            }
        }

        private DreamValue CreateMapObject(MapObjectJson mapObject) {
            DreamObjectDefinition definition = _dreamManager.ObjectTree.GetObjectDefinition(mapObject.Type);
            if (mapObject.VarOverrides?.Count > 0) {
                definition = new DreamObjectDefinition(definition);

                foreach (KeyValuePair<string, object> varOverride in mapObject.VarOverrides) {
                    if (definition.HasVariable(varOverride.Key)) {
                        definition.Variables[varOverride.Key] = _dreamManager.ObjectTree.GetDreamValueFromJsonElement(varOverride.Value);
                    }
                }
            }

            return DreamObject.CreateWrappedObject(definition);
        }
    }
}
