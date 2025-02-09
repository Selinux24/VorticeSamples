
namespace PrimalLike.EngineAPI
{
    public abstract class InputSystemBase
    {
        public abstract void OnEvent(InputSources type, InputCodes code, ref InputValue inputValue);
        public abstract void OnEvent(ulong binding, ref InputValue inputValue);

        protected InputSystemBase()
        {
            Input.AddInputCallback(this);
        }
        ~InputSystemBase()
        {
            Input.RemoveInputCallback(this);
        }
    }
}
