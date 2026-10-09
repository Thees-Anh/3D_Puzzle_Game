using System.Collections.Generic;
using PuzzleRoom.Interaction;
using UnityEngine;

namespace PuzzleRoom.Puzzles.Hanoi
{
    public sealed class HanoiPeg : MonoBehaviour, IInteractable
    {
        [SerializeField] private string pegName = "Peg";
        [SerializeField] private HanoiPuzzleManager puzzleManager;
        [SerializeField] private Transform[] slots;

        private readonly List<HanoiBook> books = new List<HanoiBook>();

        public string PegName => pegName;
        public int BookCount => books.Count;
        public HanoiBook TopBook => books.Count == 0 ? null : books[books.Count - 1];
        public string InteractionPrompt => $"Move to {pegName}";
        public bool CanInteract => puzzleManager != null && puzzleManager.CanInteractWithPeg(this);

        public void Interact(GameObject interactor)
        {
            puzzleManager.TryMoveSelectedTo(this);
        }

        public bool CanReceive(HanoiBook book)
        {
            return book != null && (TopBook == null || (int)book.Size < (int)TopBook.Size);
        }

        public HanoiBook GetBookAt(int index)
        {
            return index >= 0 && index < books.Count ? books[index] : null;
        }

        public Vector3 GetSlotPosition(int index)
        {
            if (slots != null && index >= 0 && index < slots.Length && slots[index] != null)
            {
                return slots[index].position;
            }

            return transform.position + Vector3.up * (0.15f * index);
        }

        public void AddBook(HanoiBook book, bool snapToSlot)
        {
            books.Add(book);
            book.CurrentPeg = this;

            if (snapToSlot)
            {
                book.transform.position = GetSlotPosition(books.Count - 1);
            }
        }

        public void RemoveTopBook(HanoiBook book)
        {
            if (TopBook == book)
            {
                books.RemoveAt(books.Count - 1);
            }
        }

        public void ClearBooks()
        {
            books.Clear();
        }
    }
}
