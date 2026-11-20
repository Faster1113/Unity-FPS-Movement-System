// PlayerInputState.cs
using UnityEngine;

namespace Game.Player
{
    /// <summary>Снимок ввода за кадр. Struct — без аллокаций.</summary>
    public struct PlayerInputState
    {
        public Vector2 Move;        // сырой ввод (-1..1)
        public Vector3 WorldMove;   // относительно камеры, нормализованный, y = 0
        public bool Run;
        public bool Crouch;
        public bool JumpPressed;
        public bool JumpHeld;
        public bool SlidePressed;
        public bool DashPressed;
        public bool InteractPressed;
        public bool AimHeld;
    }
}
