﻿using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;
using System.Text;

namespace MHServerEmu.Games.Navi
{
    public class NaviSystem
    {
        public bool Log = true;
        private static readonly Logger Logger = LogManager.CreateLogger();
        public Region Region { get; private set; }
        public List<NaviErrorReport> ErrorLog { get; private set; } = new ();

        public bool Initialize(Region region)
        {
            Region = region;
            return true;
        }

        public void LogError(string msg)
        {
            NaviErrorReport errorReport = new()
            {
                Msg = msg
            };
            ErrorLog.Add(errorReport);
        }

        public void LogError(string msg, NaviEdge edge)
        {
            NaviErrorReport errorReport = new()
            {
                Msg = msg,
                Edge = edge
            };
            ErrorLog.Add(errorReport);
        }

        public void LogError(string msg, NaviPoint point)
        {
            NaviErrorReport errorReport = new()
            {
                Msg = msg,
                Point = point
            };
            ErrorLog.Add(errorReport);
        }

        public void ClearErrorLog()
        {
            ErrorLog.Clear();
        }

        public bool CheckErrorLog(bool clearErrorLog, string info = null)
        {
            bool hasErrors = HasErrors();
            if (Log && hasErrors)
            {
                var error = ErrorLog.First();

                Cell cell = null;
                if (Region != null)
                {
                    if (error.Point != null)
                        cell = Region.GetCellAtPosition(error.Point.Pos);
                    else if (error.Edge != null)
                        cell = Region.GetCellAtPosition(error.Edge.Midpoint());
                }
                StringBuilder sb = new();
                sb.AppendLine($"Navigation Error: {error.Msg}");
                sb.AppendLine($"Cell: {(cell != null ? cell.ToString() : "Unknown")}");
                if (error.Point != null)
                    sb.AppendLine($"Point: {error.Point}");
                if (error.Edge != null)
                    sb.AppendLine($"Edge: {error.Edge}");
                if (string.IsNullOrEmpty(info) == false)
                    sb.AppendLine($"Extra Info: {info}");
                Logger.Error(sb.ToString());
            }

            if (clearErrorLog) ClearErrorLog();
            return hasErrors;
        }

        public bool HasErrors()
        {
            return ErrorLog.Any();
        }
    }

    public struct NaviErrorReport
    { 
        public string Msg;
        public NaviPoint Point;
        public NaviEdge Edge;
    }

    public class NaviPathSearchState
    {
        public NaviPathSearchState()
        {
        }
    }

    [Flags]
    public enum NaviTriangleFlags
    {
        Attached = 1 << 0,
    }

    public class NaviTriangle
    {
        public NaviEdge[] Edges { get; set; }
        public int EdgeSideFlags { get; private set; }
        public NaviTriangleFlags Flags { get; private set; }

        public NaviTriangle(NaviEdge e0, NaviEdge e1, NaviEdge e2)
        {
            Edges = new NaviEdge[3];
            Edges[0] = e0;
            Edges[1] = e1;
            Edges[2] = e2;
            UpdateEdgeSideFlags();
            Attach();
        }

        public void Attach()
        {
            for (int i = 0; i < 3; i++)
                Edges[i].AttachTriangle(this);

            Flags |= NaviTriangleFlags.Attached;
        }

        public void Detach()
        {
            if (Flags.HasFlag(NaviTriangleFlags.Attached))
            {
                for (int i = 0; i < 3; i++)
                    Edges[i].DetachTriangle(this);

                Flags &= ~NaviTriangleFlags.Attached;
            }
        }

        public Vector3 Centroid()
        {
            return (PointCW(0).Pos + PointCW(1).Pos + PointCW(2).Pos) / 3.0f;
        }

        public NaviEdge Edge(int index)
        {
            return Edges[index];
        }

        public void UpdateEdgeSideFlags()
        {
            EdgeSideFlags = 0;

            if (Edges[0].Points[0] == Edges[1].Points[0] || Edges[0].Points[1] == Edges[1].Points[1])
                EdgeSideFlags |= (1 << 1); 

            if (Edges[0].Points[0] == Edges[2].Points[0] || Edges[0].Points[1] == Edges[2].Points[1])
                EdgeSideFlags |= (1 << 2);

            if (EdgePointCW(0, 1) != EdgePointCW(1, 0))
                EdgeSideFlags = (~EdgeSideFlags) & 0x07;
        }

        private NaviPoint EdgePointCW(int edgeIndex, int point)
        {
            int pointIndex = EdgeSideFlag(edgeIndex) ^ point;
            return Edges[edgeIndex].Points[pointIndex];
        }

        private NaviPoint PointCW(int edgeIndex)
        {
            int pointIndex = EdgeSideFlag(edgeIndex);
            return Edges[edgeIndex].Points[pointIndex];
        }

        private int EdgeSideFlag(int edgeIndex)
        {
            return (EdgeSideFlags >> edgeIndex) & 1;
        }
    }

    [Flags]
    public enum NaviEdgeFlags
    {
        None = 0,
        Flag0 = 1 << 0,
        Flag1 = 1 << 1,
        Flag2 = 1 << 2,
        Flag3 = 1 << 3,
    }

    public class NaviEdge
    {
        public NaviEdgeFlags EdgeFlags { get; set; }
        public NaviEdgePathingFlags PathingFlags { get; set; }
        public NaviPoint[] Points { get; set; }
        public NaviTriangle[] Triangles { get; set; }

        public NaviEdge(NaviPoint p0, NaviPoint p1, NaviEdgeFlags edgeFlags, NaviEdgePathingFlags pathingFlags)
        {
            EdgeFlags = edgeFlags;
            PathingFlags = pathingFlags;
            Points = new NaviPoint[2];
            Points[0] = p0;
            Points[1] = p1;
            Triangles = new NaviTriangle[2];
        }

        public void AttachTriangle(NaviTriangle triangle)
        {
            if (Triangles[0] == null) 
                Triangles[0] = triangle;
            else 
                Triangles[1] = triangle;
        }

        public void DetachTriangle(NaviTriangle triangle)
        {
            if (Triangles[0] == triangle)
                Triangles[0] = null;
            else
                Triangles[1] = null;
        }

        public NaviTriangle OpposedTriangle(NaviTriangle triangle)
        {
            if (Triangles[0] == triangle) 
                return Triangles[1];
            else 
                return Triangles[0];
        }

        public Vector3 Midpoint()
        {
            return (Points[0].Pos + Points[1].Pos) / 2.0f;
        }

        public override string ToString()
        {
            return $"NaviEdge [p0={Points[0]} p1={Points[1]}]";
        }

    }

    public class NaviEdgePathingFlags
    {
        private readonly NaviContentFlags[] _flags;

        public NaviEdgePathingFlags()
        {
            _flags = new NaviContentFlags[2];
            Clear();
        }

        public NaviEdgePathingFlags(NaviContentFlags[] flags0, NaviContentFlags[] flags1)
        {
            _flags = new NaviContentFlags[2];
            foreach (var flag in flags0) _flags[0] |= flag;
            foreach (var flag in flags1) _flags[1] |= flag;
        }

        public void Clear()
        {
            _flags[0] = NaviContentFlags.None;
            _flags[1] = NaviContentFlags.None;
        }

        public void Clear(int side)
        {
            _flags[side] = NaviContentFlags.None;
        }

        public NaviContentFlags GetContentFlagsForSide(int side)
        {
            return _flags[side];
        }

        public void Merge(NaviEdgePathingFlags other, bool flipEdgePathFlags)
        {
            int side0 = flipEdgePathFlags ? 0 : 1;
            int side1 = flipEdgePathFlags ? 1 : 0;
            _flags[0] |= other._flags[side0];
            _flags[1] |= other._flags[side1];
        }
    }

    public enum NaviPointFlags 
    {
        None,
        Attached 
    }

    public class NaviPoint
    { 
        public Vector3 Pos { get; internal set; }
        public NaviPointFlags Flags { get; private set; }
        public int Influence { get; private set; }
        public float InfluenceRadius { get; private set; }

        public NaviPoint(Vector3 pos)
        {
            Pos = pos;
        }

        public override string ToString()
        {
            return $"NaviPoint ({Pos.X:F4} {Pos.Y:F4} {Pos.Z:F4}) flg:{Flags} inf:{Influence}";
        }

    }

}
