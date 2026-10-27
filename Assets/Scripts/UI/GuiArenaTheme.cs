using UnityEngine;
using UnityEngine.UI;
namespace TapBlast
{
    /// <summary>Per-level pastel arena backdrop inside the white arena card (sky, mint or peach theme).
    /// The global background keeps its authored gradient.</summary>
    public sealed class GuiArenaTheme : MonoBehaviour
    {
        [SerializeField] private Image arena, background;
        [SerializeField] private GameObject arenaRim;
        [SerializeField] private Sprite city, valley, waterfall;
        [Tooltip("Tint multiplied over the arena backdrop.")]
        [SerializeField] private Color sceneryTint = Color.white;
        public void Refresh(int level)
        {
            arena.sprite = level == 8 ? waterfall : level == 1 || level > 10 ? city : valley;
            arena.color = sceneryTint;
            arenaRim.SetActive(true);
        }
    }
}
