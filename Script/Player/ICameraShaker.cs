// ICameraShaker.cs — контракт камеры, без жёсткой связи с конкретным классом
namespace Game.Player
{
    public interface ICameraShaker
    {
        void Shake(float intensity, float duration);
    }
}
