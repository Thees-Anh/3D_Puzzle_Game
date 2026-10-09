using System;
using System.Collections;
using System.Linq;
using PuzzleRoom.Interaction;
using PuzzleRoom.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleRoom.Puzzles.Hanoi
{
    public sealed class HanoiPuzzleManager : PuzzleBase
    {
        [Header("Puzzle Pieces")]
        [SerializeField] private HanoiPeg leftPeg;
        [SerializeField] private HanoiPeg middlePeg;
        [SerializeField] private HanoiPeg rightPeg;
        [SerializeField] private HanoiBook[] books;

        [Header("Movement")]
        [SerializeField, Min(0.01f)] private float moveSpeed = 1.8f;
        [SerializeField, Min(0.1f)] private float travelHeight = 0.75f;

        [Header("Lock")]
        [SerializeField] private bool startUnlocked;
        [SerializeField] private GameObject unlockedIndicator;

        [Header("UI")]
        [SerializeField] private Text moveCounterText;

        [Header("Secret Compartment")]
        [SerializeField] private Transform secretDrawer;
        [SerializeField] private Vector3 drawerOpenOffset = new Vector3(-0.65f, 0f, 0f);
        [SerializeField, Min(0.01f)] private float drawerSpeed = 0.7f;
        [SerializeField] private GameObject rewardObject;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs;

        private HanoiBook selectedBook;
        private int moveCount;
        private bool isAnimating;
        private bool isUnlocked;
        private Vector3 drawerClosedPosition;

        public int MoveCount => moveCount;
        public int MinimumMoves => books == null ? 0 : (1 << books.Length) - 1;
        public bool IsUnlocked => isUnlocked;
        public bool CanReset => isUnlocked && !IsCompleted && !isAnimating;

        public void RestoreUnlockOnly()
        {
            isUnlocked = true;
            if (unlockedIndicator != null) unlockedIndicator.SetActive(true);
        }

        public int[] CapturePegIndices()
        {
            if (books == null) return Array.Empty<int>();
            int[] result = new int[books.Length];
            for (int i = 0; i < books.Length; i++)
                result[i] = books[i] == null ? 0 : books[i].CurrentPeg == middlePeg ? 1 : books[i].CurrentPeg == rightPeg ? 2 : 0;
            return result;
        }

        public void RestoreState(bool unlocked, bool completed, int savedMoveCount, int[] pegIndices)
        {
            StopAllCoroutines();
            isAnimating = false;
            selectedBook = null;
            isUnlocked = unlocked;
            RestoreCompletion(completed);
            if (unlockedIndicator != null) unlockedIndicator.SetActive(unlocked);
            leftPeg?.ClearBooks(); middlePeg?.ClearBooks(); rightPeg?.ClearBooks();
            if (books != null)
            {
                for (int peg = 0; peg < 3; peg++)
                    foreach (HanoiBook book in books.Where((book, i) => book != null && (pegIndices == null || i >= pegIndices.Length ? 0 : pegIndices[i]) == peg).OrderByDescending(book => (int)book.Size))
                        (peg == 1 ? middlePeg : peg == 2 ? rightPeg : leftPeg)?.AddBook(book, true);
            }
            moveCount = Mathf.Max(0, savedMoveCount);
            UpdateMoveCounter();
            if (secretDrawer != null) secretDrawer.localPosition = completed ? drawerClosedPosition + drawerOpenOffset : drawerClosedPosition;
            if (rewardObject != null) rewardObject.SetActive(completed);
        }

        private void Awake()
        {
            if (secretDrawer != null)
            {
                drawerClosedPosition = secretDrawer.localPosition;
            }

            isUnlocked = startUnlocked;
            if (unlockedIndicator != null)
            {
                unlockedIndicator.SetActive(isUnlocked);
            }

            ResetPuzzleImmediate();
        }

        public bool CanInteractWithBook(HanoiBook book)
        {
            if (book == null || IsCompleted || isAnimating)
            {
                return false;
            }

            return selectedBook == null || selectedBook == book;
        }

        public bool CanInteractWithPeg(HanoiPeg peg)
        {
            return isUnlocked && peg != null && selectedBook != null && !IsCompleted && !isAnimating;
        }

        public void HandleBookInteraction(HanoiBook book)
        {
            if (!CanInteractWithBook(book))
            {
                return;
            }

            if (!isUnlocked)
            {
                GameAudio.PlayAt(AudioCue.DoorLocked, transform.position);
                InteractionFeedback.ShowMessage("The books are locked in place.", 1.4f);
                return;
            }

            if (selectedBook == book)
            {
                CancelSelection("Selection Cancelled");
                return;
            }

            if (book.CurrentPeg == null || book.CurrentPeg.TopBook != book)
            {
                GameAudio.PlayAt(AudioCue.InvalidMove, book.transform.position);
                InteractionFeedback.ShowMessage("Cannot move this book", 1.2f);
                Log($"Rejected selection: {book.name} is not on top.");
                return;
            }

            selectedBook = book;
            GameAudio.PlayAt(AudioCue.BookSelect, book.transform.position);
            selectedBook.SetSelectedVisual(true);
            Log($"Selected {book.name} from {book.CurrentPeg.PegName}.");
        }

        public void TryMoveSelectedTo(HanoiPeg targetPeg)
        {
            if (!CanInteractWithPeg(targetPeg))
            {
                return;
            }

            HanoiPeg sourcePeg = selectedBook.CurrentPeg;

            if (targetPeg == sourcePeg)
            {
                CancelSelection("Selection Cancelled");
                return;
            }

            if (!targetPeg.CanReceive(selectedBook))
            {
                GameAudio.PlayAt(AudioCue.InvalidMove, targetPeg.transform.position);
                InteractionFeedback.ShowMessage("Invalid Move", 1.2f);
                Log($"Invalid move: {selectedBook.name} to {targetPeg.PegName}.");
                return;
            }

            HanoiBook movingBook = selectedBook;
            movingBook.SetSelectedVisual(false);
            selectedBook = null;

            Vector3 destination = targetPeg.GetSlotPosition(targetPeg.BookCount);
            StartCoroutine(AnimateMove(movingBook, sourcePeg, targetPeg, destination));
        }

        public void ResetPuzzle()
        {
            if (!CanReset)
            {
                return;
            }

            ResetPuzzleImmediate();
            GameAudio.PlayAt(AudioCue.PuzzleReset, transform.position);
            InteractionFeedback.ShowMessage("Puzzle Reset", 1.2f);
            Log("Puzzle reset.");
        }

        public void UnlockPuzzle()
        {
            if (isUnlocked || IsCompleted)
            {
                return;
            }

            isUnlocked = true;
            GameAudio.PlayAt(AudioCue.MechanismUnlock, transform.position);
            if (unlockedIndicator != null)
            {
                unlockedIndicator.SetActive(true);
            }

            InteractionFeedback.ShowMessage("BOOK MECHANISM UNLOCKED", 2f);
            Log("Book mechanism unlocked by symbol sequence.");
        }

        private void ResetPuzzleImmediate()
        {
            StopAllCoroutines();
            isAnimating = false;

            if (selectedBook != null)
            {
                selectedBook.SetSelectedVisual(false);
                selectedBook = null;
            }

            leftPeg?.ClearBooks();
            middlePeg?.ClearBooks();
            rightPeg?.ClearBooks();

            if (books != null && leftPeg != null)
            {
                foreach (HanoiBook book in books.Where(book => book != null).OrderByDescending(book => (int)book.Size))
                {
                    leftPeg.AddBook(book, true);
                }
            }

            moveCount = 0;
            UpdateMoveCounter();

            if (secretDrawer != null)
            {
                secretDrawer.localPosition = drawerClosedPosition;
            }

            if (rewardObject != null)
            {
                rewardObject.SetActive(false);
            }
        }

        private IEnumerator AnimateMove(
            HanoiBook book,
            HanoiPeg sourcePeg,
            HanoiPeg targetPeg,
            Vector3 destination)
        {
            isAnimating = true;
            float raisedY = Mathf.Max(book.transform.position.y, destination.y) + travelHeight;
            Vector3 raisedStart = new Vector3(book.transform.position.x, raisedY, book.transform.position.z);
            Vector3 raisedDestination = new Vector3(destination.x, raisedY, destination.z);

            yield return MoveBookTo(book.transform, raisedStart);
            yield return MoveBookTo(book.transform, raisedDestination);
            yield return MoveBookTo(book.transform, destination);

            sourcePeg.RemoveTopBook(book);
            targetPeg.AddBook(book, false);
            GameAudio.PlayAt(AudioCue.BookPlace, book.transform.position);
            moveCount++;
            isAnimating = false;
            UpdateMoveCounter();
            Log($"Move {moveCount}: {book.name} to {book.CurrentPeg.PegName}.");

            if (IsSolved())
            {
                SolvePuzzle();
            }
        }

        private IEnumerator MoveBookTo(Transform bookTransform, Vector3 target)
        {
            while ((bookTransform.position - target).sqrMagnitude > 0.000001f)
            {
                bookTransform.position = Vector3.MoveTowards(
                    bookTransform.position,
                    target,
                    moveSpeed * Time.deltaTime);
                yield return null;
            }

            bookTransform.position = target;
        }

        private bool IsSolved()
        {
            if (rightPeg == null || books == null || rightPeg.BookCount != books.Length)
            {
                return false;
            }

            for (int index = 0; index < rightPeg.BookCount; index++)
            {
                int expectedSize = books.Length - index;
                if ((int)rightPeg.GetBookAt(index).Size != expectedSize)
                {
                    return false;
                }
            }

            return true;
        }

        private void SolvePuzzle()
        {
            Complete();
            GameAudio.PlayAt(AudioCue.HanoiComplete, transform.position);
            string message = moveCount == MinimumMoves
                ? "Perfect Solve! 15 Moves"
                : $"Puzzle Solved! Completed in {moveCount} Moves";
            InteractionFeedback.ShowMessage(message, 2.5f);
            Log(message);
            StartCoroutine(OpenSecretDrawer());
        }

        private IEnumerator OpenSecretDrawer()
        {
            GameAudio.PlayAt(AudioCue.DrawerOpen, secretDrawer != null ? secretDrawer.position : transform.position);
            if (secretDrawer == null)
            {
                ShowReward();
                yield break;
            }

            Vector3 targetPosition = drawerClosedPosition + drawerOpenOffset;
            while ((secretDrawer.localPosition - targetPosition).sqrMagnitude > 0.000001f)
            {
                secretDrawer.localPosition = Vector3.MoveTowards(
                    secretDrawer.localPosition,
                    targetPosition,
                    drawerSpeed * Time.deltaTime);
                yield return null;
            }

            secretDrawer.localPosition = targetPosition;
            ShowReward();
        }

        private void ShowReward()
        {
            if (rewardObject != null)
            {
                rewardObject.SetActive(true);
            }
        }

        private void CancelSelection(string message)
        {
            selectedBook.SetSelectedVisual(false);
            selectedBook = null;
        }

        private void UpdateMoveCounter()
        {
            if (moveCounterText != null)
            {
                moveCounterText.text = $"Moves: {moveCount}\nMinimum: {MinimumMoves}";
            }
        }

        private void Log(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[Hanoi] {message}", this);
            }
        }
    }
}
