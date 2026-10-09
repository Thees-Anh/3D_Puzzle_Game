using System.Collections;
using PuzzleRoom.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleRoom.Puzzles.PowerGrid
{
    public sealed class CircuitTile : MonoBehaviour
    {
        [SerializeField] private ConnectionDirection baseConnections;
        [SerializeField, Range(0, 3)] private int initialRotation;
        [SerializeField, Range(0, 3)] private int solvedRotation;
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private RectTransform visual;
        [SerializeField] private Graphic[] wireGraphics;
        [SerializeField] private Color unpoweredColor = new Color(0.18f, 0.2f, 0.24f);
        [SerializeField] private Color poweredColor = new Color(1f, 0.75f, 0.12f);
        [SerializeField] private Color unpoweredWireColor = new Color(0.78f, 0.82f, 0.88f);
        [SerializeField] private Color poweredWireColor = new Color(1f, 0.82f, 0.15f);
        [SerializeField, Min(0.01f)] private float rotationSpeed = 540f;

        private CircuitGridManager gridManager;
        private int currentRotation;
        private bool isRotating;

        public int CurrentRotation => currentRotation;
        public int SolvedRotation => solvedRotation;
        public bool IsRotating => isRotating;
        public ConnectionDirection BaseConnections => baseConnections;
        public ConnectionDirection CurrentConnections => RotateConnections(baseConnections, currentRotation);

        public void Initialize(CircuitGridManager manager)
        {
            gridManager = manager;
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
                button.onClick.AddListener(HandleClick);
            }

            ResetTile();
        }

        public void ResetTile()
        {
            StopAllCoroutines();
            isRotating = false;
            currentRotation = initialRotation;
            if (visual != null)
            {
                visual.localRotation = Quaternion.Euler(0f, 0f, -90f * currentRotation);
            }
            SetPowered(false);
        }

        public void RestoreRotation(int rotation)
        {
            StopAllCoroutines();
            isRotating = false;
            currentRotation = Mathf.Abs(rotation) % 4;
            if (visual != null) visual.localRotation = Quaternion.Euler(0f, 0f, -90f * currentRotation);
        }

        public void SetPowered(bool powered)
        {
            if (background != null)
            {
                background.color = powered ? poweredColor * 0.35f : unpoweredColor;
            }

            if (wireGraphics != null)
            {
                foreach (Graphic wireGraphic in wireGraphics)
                {
                    if (wireGraphic != null)
                    {
                        wireGraphic.color = powered ? poweredWireColor : unpoweredWireColor;
                    }
                }
            }
        }

        private void HandleClick()
        {
            if (gridManager == null || !gridManager.CanRotateTiles || isRotating)
            {
                return;
            }

            currentRotation = (currentRotation + 1) % 4;
            GameAudio.Play(AudioCue.CircuitRotate);
            StartCoroutine(AnimateRotation());
        }

        private IEnumerator AnimateRotation()
        {
            isRotating = true;
            Quaternion target = Quaternion.Euler(0f, 0f, -90f * currentRotation);
            while (visual != null && Quaternion.Angle(visual.localRotation, target) > 0.1f)
            {
                visual.localRotation = Quaternion.RotateTowards(
                    visual.localRotation,
                    target,
                    rotationSpeed * Time.unscaledDeltaTime);
                yield return null;
            }

            if (visual != null)
            {
                visual.localRotation = target;
            }

            isRotating = false;
            gridManager.NotifyTileRotated();
        }

        private static ConnectionDirection RotateConnections(ConnectionDirection connections, int quarterTurns)
        {
            for (int turn = 0; turn < quarterTurns; turn++)
            {
                ConnectionDirection rotated = ConnectionDirection.None;
                if ((connections & ConnectionDirection.Up) != 0) rotated |= ConnectionDirection.Right;
                if ((connections & ConnectionDirection.Right) != 0) rotated |= ConnectionDirection.Down;
                if ((connections & ConnectionDirection.Down) != 0) rotated |= ConnectionDirection.Left;
                if ((connections & ConnectionDirection.Left) != 0) rotated |= ConnectionDirection.Up;
                connections = rotated;
            }

            return connections;
        }
    }
}
