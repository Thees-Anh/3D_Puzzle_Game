using UnityEngine;
using UnityEngine.UI;

namespace PuzzleRoom.UI
{
    public sealed class VictoryUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text completionTimeText;

        private void Awake()
        {
            panel?.SetActive(false);
        }

        public void Show(string formattedTime)
        {
            if (completionTimeText != null)
            {
                completionTimeText.text = formattedTime;
            }

            panel?.SetActive(true);
        }
    }
}
