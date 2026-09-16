using System;
using System.Collections.Generic;
using Scriptables;
using UnityEngine;

namespace Game
{
    public sealed class ZoneLayoutPlanner
    {
        private const double FullCircleRadians = Math.PI * 2.0;

        private readonly List<Vector3> _positions = new List<Vector3>(64);
        private readonly System.Random _random;

        public ZoneLayoutPlanner(System.Random random)
        {
            _random = random ?? new System.Random();
        }

        public IReadOnlyList<Vector3> Positions => _positions;

        public static float ResolveSpacing(SpawnZone zone, LayoutSet layout, IReadOnlyList<float> zoneRadii)
        {
            if (zone.AutoSpacing == false && zone.Spacing > 0f)
            {
                return zone.Spacing;
            }

            float maxRadius = 0f;

            for (int i = 0; i < zoneRadii.Count; i++)
            {
                maxRadius = Mathf.Max(maxRadius, zoneRadii[i]);
            }

            return Mathf.Max(0.5f, maxRadius * layout.AutoSpacingFactor);
        }

        public void Collect(SpawnZone zone, Vector2 center, float spacing, LayoutSet layout)
        {
            _positions.Clear();

            if (zone.Shape == SpawnShape.Grid)
            {
                CollectGrid(center, zone.Count, spacing);
            }
            else if (zone.Shape == SpawnShape.CircleGrid)
            {
                CollectCircleGrid(center, zone.Count, spacing);
            }
            else if (zone.Shape == SpawnShape.Circle)
            {
                CollectCircle(center, zone.Radius, zone.Count);
            }
            else
            {
                CollectScatter(zone, center, spacing, layout);
            }
        }

        private void CollectGrid(Vector2 center, int count, float spacing)
        {
            if (count <= 0)
            {
                return;
            }

            int rows = Mathf.Max(1, Mathf.RoundToInt(Mathf.Sqrt(count)));
            int columns = Mathf.CeilToInt(count / (float)rows);
            float halfWidth = (columns - 1) * spacing * 0.5f;
            float halfDepth = (rows - 1) * spacing * 0.5f;

            int placedCount = 0;

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    if (placedCount >= count)
                    {
                        break;
                    }

                    Vector3 offset = new Vector3(column * spacing - halfWidth, 0f, row * spacing - halfDepth);

                    _positions.Add(new Vector3(center.x, 0f, center.y) + offset);
                    placedCount++;
                }
            }
        }

        private void CollectCircleGrid(Vector2 center, int count, float spacing)
        {
            if (count <= 0)
            {
                return;
            }

            _positions.Add(new Vector3(center.x, 0f, center.y));

            int placedCount = 1;
            int ringIndex = 1;

            while (placedCount < count)
            {
                float ringRadius = ringIndex * spacing;
                int ringCapacity = Mathf.Max(1, Mathf.FloorToInt(2f * Mathf.PI * ringRadius / spacing));
                int pointsOnRing = Mathf.Min(ringCapacity, count - placedCount);
                float angleStep = 2f * Mathf.PI / pointsOnRing;

                for (int i = 0; i < pointsOnRing; i++)
                {
                    float angle = angleStep * i;
                    float offsetX = center.x + Mathf.Cos(angle) * ringRadius;
                    float offsetZ = center.y + Mathf.Sin(angle) * ringRadius;

                    _positions.Add(new Vector3(offsetX, 0f, offsetZ));
                    placedCount++;
                }

                ringIndex++;
            }
        }

        private void CollectCircle(Vector2 center, float radius, int count)
        {
            if (count <= 0)
            {
                return;
            }

            float angleStep = 2f * Mathf.PI / count;

            for (int i = 0; i < count; i++)
            {
                float angle = angleStep * i;
                float offsetX = center.x + Mathf.Cos(angle) * radius;
                float offsetZ = center.y + Mathf.Sin(angle) * radius;

                _positions.Add(new Vector3(offsetX, 0f, offsetZ));
            }
        }

        private void CollectScatter(SpawnZone zone, Vector2 center, float spacing, LayoutSet layout)
        {
            float minDistance = spacing * layout.ScatterDistanceFactor;
            float minDistanceSqr = minDistance * minDistance;
            int attemptsLimit = zone.Count * 10;
            int attempts = 0;

            while (_positions.Count < zone.Count && attempts < attemptsLimit)
            {
                attempts++;

                double angle = _random.NextDouble() * FullCircleRadians;
                double distance = zone.Radius * Math.Sqrt(_random.NextDouble());

                Vector3 candidate = new Vector3(
                    center.x + Mathf.Cos((float)angle) * (float)distance,
                    0f,
                    center.y + Mathf.Sin((float)angle) * (float)distance);

                if (IsFarEnough(candidate, minDistanceSqr) == true)
                {
                    _positions.Add(candidate);
                }
            }
        }

        private bool IsFarEnough(Vector3 candidate, float minDistanceSqr)
        {
            for (int i = 0; i < _positions.Count; i++)
            {
                Vector3 delta = candidate - _positions[i];
                delta.y = 0f;

                if (delta.sqrMagnitude < minDistanceSqr)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
