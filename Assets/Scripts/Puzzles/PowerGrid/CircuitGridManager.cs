using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleRoom.Puzzles.PowerGrid
{
    [Serializable]
    public sealed class CircuitOutput
    {
        public PowerChannel channel;
        public int row;
        public int column;
        public ConnectionDirection edgeDirection;
        public Text statusText;
    }

    public sealed class CircuitGridManager : MonoBehaviour
    {
        [SerializeField] private int rows = 4;
        [SerializeField] private int columns = 4;
        [SerializeField] private CircuitTile[] tiles;
        [SerializeField] private int sourceRow;
        [SerializeField] private int sourceColumn = 1;
        [SerializeField] private ConnectionDirection sourceEdge = ConnectionDirection.Up;
        [SerializeField] private CircuitOutput[] outputs;
        [SerializeField] private Text rotationCounterText;
        [SerializeField] private Text poweredCounterText;
        [SerializeField] private bool[] requiredRouteTiles;

        private bool[] poweredTiles;
        private int rotations;

        public bool IsSolved { get; private set; }
        public bool IsEnabled { get; set; }
        public bool CanRotateTiles => IsEnabled && !IsSolved && !AnyTileIsRotating();
        public int Rotations => rotations;

        public int[] CaptureRotations() => tiles == null ? Array.Empty<int>() : Array.ConvertAll(tiles, tile => tile != null ? tile.CurrentRotation : 0);

        public void RestoreState(bool enabled, bool solved, int savedRotations, int[] tileRotations)
        {
            IsEnabled = enabled;
            IsSolved = false;
            rotations = Mathf.Max(0, savedRotations);
            if (tiles != null)
                for (int i = 0; i < tiles.Length; i++) tiles[i]?.RestoreRotation(tileRotations != null && i < tileRotations.Length ? tileRotations[i] : 0);
            UpdateCounter();
            EvaluateNetwork();
            IsSolved = solved;
        }

        public event Action GridChanged;
        public event Action RoutingSolved;

        private void Awake()
        {
            poweredTiles = new bool[rows * columns];
            if (tiles != null)
            {
                foreach (CircuitTile tile in tiles)
                {
                    tile?.Initialize(this);
                }
            }
            UpdateCounter();
            EvaluateNetwork();
        }

        public void NotifyTileRotated()
        {
            if (!CanRotateTiles)
            {
                return;
            }

            rotations++;
            UpdateCounter();
            EvaluateNetwork();
            GridChanged?.Invoke();
        }

        public void ResetGrid()
        {
            if (IsSolved)
            {
                return;
            }

            rotations = 0;
            if (tiles != null)
            {
                foreach (CircuitTile tile in tiles)
                {
                    tile?.ResetTile();
                }
            }

            UpdateCounter();
            EvaluateNetwork();
            GridChanged?.Invoke();
        }

        public void EvaluateNetwork()
        {
            if (poweredTiles == null || poweredTiles.Length != rows * columns)
            {
                poweredTiles = new bool[rows * columns];
            }
            Array.Clear(poweredTiles, 0, poweredTiles.Length);

            int sourceIndex = ToIndex(sourceRow, sourceColumn);
            if (IsValidIndex(sourceIndex) && HasConnection(tiles[sourceIndex], sourceEdge))
            {
                Queue<int> queue = new Queue<int>();
                poweredTiles[sourceIndex] = true;
                queue.Enqueue(sourceIndex);

                while (queue.Count > 0)
                {
                    int current = queue.Dequeue();
                    int row = current / columns;
                    int column = current % columns;
                    TryVisitNeighbor(queue, current, row - 1, column, ConnectionDirection.Up, ConnectionDirection.Down);
                    TryVisitNeighbor(queue, current, row, column + 1, ConnectionDirection.Right, ConnectionDirection.Left);
                    TryVisitNeighbor(queue, current, row + 1, column, ConnectionDirection.Down, ConnectionDirection.Up);
                    TryVisitNeighbor(queue, current, row, column - 1, ConnectionDirection.Left, ConnectionDirection.Right);
                }
            }

            for (int index = 0; tiles != null && index < tiles.Length; index++)
            {
                tiles[index]?.SetPowered(index < poweredTiles.Length && poweredTiles[index]);
            }

            int requiredPoweredCount = 0;
            int requiredTileCount = 0;
            for (int index = 0; index < poweredTiles.Length; index++)
            {
                bool required = requiredRouteTiles != null
                    && index < requiredRouteTiles.Length
                    && requiredRouteTiles[index];
                if (!required) continue;
                requiredTileCount++;
                if (poweredTiles[index]) requiredPoweredCount++;
            }
            if (poweredCounterText != null)
            {
                poweredCounterText.text = $"Route: {requiredPoweredCount}/{requiredTileCount}";
            }

            bool allOutputsPowered = outputs != null && outputs.Length > 0;
            if (outputs != null)
            {
                foreach (CircuitOutput output in outputs)
                {
                    int index = ToIndex(output.row, output.column);
                    bool powered = IsValidIndex(index)
                        && poweredTiles[index]
                        && (output.edgeDirection == ConnectionDirection.None
                            || HasConnection(tiles[index], output.edgeDirection));
                    if (output.statusText != null)
                    {
                        output.statusText.text = $"{output.channel}: {(powered ? "ON" : "OFF")}";
                        output.statusText.color = powered ? new Color(0.3f, 1f, 0.35f) : new Color(1f, 0.35f, 0.35f);
                    }
                    allOutputsPowered &= powered;
                }
            }

            bool allRequiredTilesPowered = requiredTileCount > 0 && requiredPoweredCount == requiredTileCount;
            if (allOutputsPowered && allRequiredTilesPowered && !IsSolved)
            {
                IsSolved = true;
                RoutingSolved?.Invoke();
            }
        }

        private void TryVisitNeighbor(
            Queue<int> queue,
            int currentIndex,
            int neighborRow,
            int neighborColumn,
            ConnectionDirection outgoing,
            ConnectionDirection reciprocal)
        {
            int neighborIndex = ToIndex(neighborRow, neighborColumn);
            if (!IsValidIndex(neighborIndex) || poweredTiles[neighborIndex])
            {
                return;
            }

            if (HasConnection(tiles[currentIndex], outgoing) && HasConnection(tiles[neighborIndex], reciprocal))
            {
                poweredTiles[neighborIndex] = true;
                queue.Enqueue(neighborIndex);
            }
        }

        private int ToIndex(int row, int column)
        {
            return row < 0 || row >= rows || column < 0 || column >= columns
                ? -1
                : row * columns + column;
        }

        private bool IsValidIndex(int index)
        {
            return tiles != null && index >= 0 && index < tiles.Length && tiles[index] != null;
        }

        private static bool HasConnection(CircuitTile tile, ConnectionDirection direction)
        {
            return tile != null && (tile.CurrentConnections & direction) != 0;
        }

        private void UpdateCounter()
        {
            if (rotationCounterText != null)
            {
                rotationCounterText.text = $"Rotations: {rotations}";
            }
        }

        private bool AnyTileIsRotating()
        {
            if (tiles == null)
            {
                return false;
            }

            foreach (CircuitTile tile in tiles)
            {
                if (tile != null && tile.IsRotating)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
