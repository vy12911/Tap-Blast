using System;
using UnityEngine;
using UnityEngine.EventSystems;
namespace TapBlast
{
    public sealed class ArenaTapSurface : MonoBehaviour, IPointerDownHandler
    {
        private Action empty;
        public void Bind(Action handler) { empty = handler; }
        public void OnPointerDown(PointerEventData data) { if (TapInput.Accept(data)) empty?.Invoke(); }
    }
}
