﻿using MHServerEmu.Games.GameData.Prototypes.Markers;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Generators.Regions;
using MHServerEmu.Games.Regions;
using MHServerEmu.Common;
using MHServerEmu.Games.Generators.Prototypes;
using MHServerEmu.Games.Common;

namespace MHServerEmu.Games.Generators.Areas
{
    public class StaticAreaCellGenerator : Generator
    {
        public StaticAreaCellGenerator(){}

        public override Aabb PreGenerate(GRandom random)
        {
            Aabb bounds = new(Aabb.InvertedLimit);

            DistrictPrototype protoDistrict = GetDistrictPrototype();
            if (protoDistrict == null || protoDistrict.CellMarkerSet == null) return bounds;

            foreach (var cellMarker in protoDistrict.CellMarkerSet)
            {
                if (cellMarker == null) continue;

                ulong cellRef = GameDatabase.GetPrototypeRefByName(cellMarker.Resource);
                if (cellRef == 0)
                {
                    Logger.Warn($"Unable to link Resource {cellMarker.Resource} to a corresponding .cell file");
                    continue;
                }

                CellPrototype cellProto = GameDatabase.GetPrototype<CellPrototype>(cellRef);
                if (cellProto == null) continue;

                bounds += cellProto.Boundbox + cellMarker.Position;

            }

            PreGenerated = true;

            return bounds;
        }

        public override bool Generate(GRandom random, RegionGenerator regionGenerator, List<ulong> areas)
        {
            
            DistrictPrototype protoDistrict = GetDistrictPrototype();
            if (protoDistrict == null) return false;

            foreach (var cellMarker in protoDistrict.CellMarkerSet)
            {
                if (cellMarker == null) continue;

                ulong cellRef = GameDatabase.GetPrototypeRefByName(cellMarker.Resource); // GetDataRefByResourceGuid 
                if (cellRef == 0) continue;

                CellSettings cellSettings = new()
                {
                    PositionInArea = cellMarker.Position,
                    OrientationInArea = cellMarker.Rotation,
                    CellRef = cellRef
                };

                Area.AddCell(AllocateCellId(), cellSettings);
            }

            Area area = Area;
            foreach (var cell in area.CellList)
            {
                if (cell != null)
                {
                    Vector3 origin = cell.RegionBounds.Center;
                    float x = cell.RegionBounds.Width;
                    float y = cell.RegionBounds.Length;

                    void TryCreateConnection(Vector3 direction)
                    {
                        Vector3 position = origin + direction;
                        if (area.IntersectsXY(position))
                        {
                            Cell otherCell = area.GetCellAtPosition(position);
                            if (otherCell != null) area.CreateCellConnection(cell, otherCell);
                        }
                    }

                    TryCreateConnection(new (x, 0.0f, 0.0f));
                    TryCreateConnection(new (-x, 0.0f, 0.0f));
                    TryCreateConnection(new (0.0f, y, 0.0f));
                    TryCreateConnection(new (0.0f, -y, 0.0f));
                }
            }

            return true;
        }

        public DistrictPrototype GetDistrictPrototype()
        {
            Area area = Area;
            if (area == null)
            {
                Logger.Warn("Unable to get SArea");
                return null;
            }

            if (Area.AreaPrototype.Generator is not DistrictAreaGeneratorPrototype proto) return null;

            ulong districtAssetRef = proto.District;
            if (districtAssetRef == 0)
            {
                Logger.Warn("StaticAreaCellGenerator called with no layout specified.");
                return null;
            }

            ulong DistrictProtoId = GameDatabase.GetDataRefByAsset(districtAssetRef);
            DistrictPrototype protoDistrict = GameDatabase.GetPrototype<DistrictPrototype>(DistrictProtoId);
            if (protoDistrict == null)
                Logger.Warn($"District Prototype is not available. Likely a missing file. Looking for Asset: {GameDatabase.GetAssetName(districtAssetRef)}");

            area.DistrictDataRef = DistrictProtoId;

            return protoDistrict;
        }

        public override bool GetPossibleConnections(ConnectionList connections, Segment segment)
        {
            connections.Clear();

            bool connected = false;
            Vector3 origin = Area.Origin;

            DistrictPrototype protoDistrict = GetDistrictPrototype();
            if (protoDistrict == null)
            {
                Logger.Warn($"StaticArea's District is Invalid");
                return false;
            }

            if (protoDistrict.CellMarkerSet == null)
            {
                Logger.Warn($"StaticArea's District contains no cells");
                return false;
            }

            foreach (var cellMarker in protoDistrict.CellMarkerSet)
            {
                ulong cellRef = GameDatabase.GetPrototypeRefByName(cellMarker.Resource);
                if (cellRef == 0) continue;

                Vector3 cellPos = cellMarker.Position;
                CellPrototype cellProto = GameDatabase.GetPrototype<CellPrototype>(cellRef);
                if (cellProto == null) continue;

                if (cellProto.MarkerSet != null)
                {
                    foreach (var marker in cellProto.MarkerSet)
                    {
                        if (marker is not CellConnectorMarkerPrototype cellConnector)  continue;

                        Vector3 connection = origin + cellPos + cellConnector.Position;

                        if (segment.Start.X == segment.End.X)
                        {
                            if (Segment.EpsilonTest(connection.X, segment.Start.X, 10.0f) &&
                               (connection.Y >= segment.Start.Y) && (connection.Y <= segment.End.Y)) 
                            {
                                connections.Add(connection);
                                connected = true;
                            }
                        }
                        else if (segment.Start.Y == segment.End.Y)
                        {
                            if (Segment.EpsilonTest(connection.Y, segment.Start.Y, 10.0f) &&
                                (connection.X >= segment.Start.X) && (connection.X <= segment.End.X))
                            {
                                connections.Add(connection);
                                connected = true;
                            }
                        }
                    }
                }
            }

            return connected;
        }

    }
}
