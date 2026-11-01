using UnityEngine;
namespace TapBlast
{
    public enum BossSignature { Sentinel, Architect }
    [CreateAssetMenu(menuName = "Tap Blast/Boss phase")]
    public sealed class BossPhaseDefinition : ScriptableObject
    {
        public string phaseId, teachingHint;
        public BossSignature signature;
        public Vector2 corePosition = new Vector2(0, .68f);
        public float warning = .45f, exposedSeconds = 1.6f, shieldSeconds = 1.2f;
        public int coreHits = 1;
        public bool safeTeaching;
    }
}
