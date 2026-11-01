using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
namespace TapBlast
{
    public static class TapInput
    {
        public static bool Accept(PointerEventData pointer)
        {
            if (pointer.button != PointerEventData.InputButton.Left) return false;
            if (pointer is ExtendedPointerEventData touch && touch.pointerType == UIPointerType.Touch && Touchscreen.current != null)
                return touch.touchId == Touchscreen.current.primaryTouch.touchId.ReadValue();
            return true;
        }
        public static void Record(TargetView target, PointerEventData pointer)
        {
            float timestamp = (float)Time.realtimeSinceStartupAsDouble;
            if (pointer is ExtendedPointerEventData touch && touch.pointerType == UIPointerType.Touch && Touchscreen.current != null)
                timestamp = (float)Touchscreen.current.primaryTouch.startTime.ReadValue();
            CampaignTelemetry.Input(target.StableId, pointer.pointerId, pointer.position, target.Position,
                target.LaneWarning, timestamp);
        }
    }
}
