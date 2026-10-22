using UnityEngine;
using UnityEngine.UI;
namespace TapBlast
{
    public sealed class GuiArenaTheme : MonoBehaviour
    {
        [SerializeField] private Image arena, background;
        [SerializeField] private GameObject arenaRim;
        [SerializeField] private Sprite city, valley, waterfall;
        public void Refresh(int level)
        {
            bool framed = level == 1 || level > 10;
            arena.sprite = city;
            arena.color = framed ? Color.white : Color.clear;
            arenaRim.SetActive(framed);
            background.sprite = level == 8 ? waterfall : valley;
        }
    }
}
