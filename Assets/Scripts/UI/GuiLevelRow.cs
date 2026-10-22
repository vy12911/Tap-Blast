using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TapBlast
{
    // Saved on the ten reusable level rows. Progress is refreshed only on navigation.
    public sealed class GuiLevelRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text number;
        [SerializeField] private Image[] stars;
        public void Refresh(int level, int earned)
        {
            number.text = level.ToString("00");
            for (int i = 0; i < stars.Length; i++)
                stars[i].color = i < earned ? Color.white : new Color(0.38f, 0.48f, 0.58f, .45f);
        }
    }
}
