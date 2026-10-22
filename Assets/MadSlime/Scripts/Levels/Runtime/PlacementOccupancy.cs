using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class PlacementOccupancy
    {
        private const float CellSize = 8f;

        private Dictionary<Vector2Int, List<int>> _cells = new Dictionary<Vector2Int, List<int>>();
        private List<Vector3> _positions = new List<Vector3>();
        private List<float> _radii = new List<float>();
        private List<float> _gaps = new List<float>();

        public void Clear()
        {
            foreach (List<int> occupants in _cells.Values)
            {
                occupants.Clear();
            }

            _positions.Clear();
            _radii.Clear();
            _gaps.Clear();
        }

        public bool IsSpaceFree(Vector3 position, float radius, float gap)
        {
            float extent = radius + gap;
            Vector2Int minimum = GetCell(position.x - extent, position.z - extent);
            Vector2Int maximum = GetCell(position.x + extent, position.z + extent);

            for (int column = minimum.x; column <= maximum.x; column++)
            {
                for (int row = minimum.y; row <= maximum.y; row++)
                {
                    Vector2Int cell = new Vector2Int(column, row);

                    if (_cells.TryGetValue(cell, out List<int> occupants) == false)
                    {
                        continue;
                    }

                    for (int i = 0; i < occupants.Count; i++)
                    {
                        int occupantIndex = occupants[i];
                        Vector3 offset = position - _positions[occupantIndex];
                        offset.y = 0f;
                        float clearance = radius + _radii[occupantIndex] + Mathf.Max(gap, _gaps[occupantIndex]);

                        if (offset.sqrMagnitude < clearance * clearance)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        public void Add(Vector3 position, float radius, float gap)
        {
            int occupantIndex = _positions.Count;
            _positions.Add(position);
            _radii.Add(radius);
            _gaps.Add(gap);

            float extent = radius + gap;
            Vector2Int minimum = GetCell(position.x - extent, position.z - extent);
            Vector2Int maximum = GetCell(position.x + extent, position.z + extent);

            for (int column = minimum.x; column <= maximum.x; column++)
            {
                for (int row = minimum.y; row <= maximum.y; row++)
                {
                    Vector2Int cell = new Vector2Int(column, row);

                    if (_cells.TryGetValue(cell, out List<int> occupants) == false)
                    {
                        occupants = new List<int>();
                        _cells.Add(cell, occupants);
                    }

                    occupants.Add(occupantIndex);
                }
            }
        }

        private Vector2Int GetCell(float positionX, float positionZ)
        {
            return new Vector2Int(Mathf.FloorToInt(positionX / CellSize), Mathf.FloorToInt(positionZ / CellSize));
        }
    }
}
