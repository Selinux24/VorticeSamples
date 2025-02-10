using System.Collections.Generic;
using System.Diagnostics;

namespace PrimalLike.EngineAPI
{
    public sealed class InputSystem<T> : InputSystemBase
    {
        public delegate void InputCallbackDelegate(InputSources type, InputCodes code, ref InputValue inputValue);
        public delegate void BindingCallbackDelegate(ulong binding, ref InputValue inputValue);

        struct InputCallback
        {
            public T Instance;
            public InputCallbackDelegate Callback;
        }

        struct BindingCallback
        {
            public ulong Binding;
            public T Instance;
            public BindingCallbackDelegate Callback;
        }

        readonly List<InputCallback>[] inputCallbacks;
        readonly List<BindingCallback> bindingCallbacks;

        public InputSystem() : base()
        {
            inputCallbacks = new List<InputCallback>[(int)InputSources.Count];
            for (int i = 0; i < (int)InputSources.Count; i++)
            {
                inputCallbacks[i] = [];
            }

            bindingCallbacks = [];
        }

        public void AddHandler(InputSources type, T instance, InputCallbackDelegate callback)
        {
            Debug.Assert(instance != null && callback != null && type < InputSources.Count);
            var collection = inputCallbacks[(uint)type];
            foreach (var func in collection)
            {
                // If handler was already added then don't add it again.
                if (instance.Equals(func.Instance) && func.Callback == callback)
                {
                    return;
                }
            }

            collection.Add(new()
            {
                Instance = instance,
                Callback = callback
            });
        }
        public void AddHandler(ulong binding, T instance, BindingCallbackDelegate callback)
        {
            Debug.Assert(instance != null && callback != null);
            foreach (var func in bindingCallbacks)
            {
                // If handler was already added then don't add it again.
                if (func.Binding == binding && instance.Equals(func.Instance) && func.Callback == callback)
                {
                    return;
                }
            }

            bindingCallbacks.Add(new()
            {
                Binding = binding,
                Instance = instance,
                Callback = callback
            });
        }

        public override void OnEvent(InputSources type, InputCodes code, ref InputValue value)
        {
            Debug.Assert(type < InputSources.Count);
            foreach (var item in inputCallbacks[(uint)type])
            {
                item.Callback(type, code, ref value);
            }
        }
        public override void OnEvent(ulong binding, ref InputValue value)
        {
            foreach (var item in bindingCallbacks)
            {
                if (item.Binding == binding)
                {
                    item.Callback(binding, ref value);
                }
            }
        }
    }
}
