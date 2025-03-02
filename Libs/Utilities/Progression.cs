
namespace Utilities
{
    /// <summary>
    /// Progression class to handle progression of a task
    /// </summary>
    /// <param name="callback">Progression callback</param>
    public class Progression(Progression.ProgressCallback callback)
    {
        public delegate void ProgressCallback(uint value, uint maxValue);

        private readonly ProgressCallback callback = callback;
        private uint value = 0;
        private uint maxValue = 0;

        public uint MaxValue => maxValue;
        public uint Value => value;

        public void Callback(uint value, uint maxValue)
        {
            this.value = value;
            this.maxValue = maxValue;
            callback?.Invoke(value, maxValue);
        }
    }
}
