using PrimalLike.EngineAPI;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace PrimalLike
{
    public static class Input
    {
        private static readonly Dictionary<ulong, InputValue> inputValues = [];
        private static readonly Dictionary<ulong, InputBinding> inputBindings = [];
        private static readonly Dictionary<ulong, ulong> sourceBindingMap = [];
        private static readonly List<InputSystemBase> inputCallbacks = [];

        public static void AddInputCallback(InputSystemBase callback)
        {
            Debug.Assert(callback != null);
            inputCallbacks.Add(callback);
        }
        public static void RemoveInputCallback(InputSystemBase callback)
        {
            Debug.Assert(callback != null);
            inputCallbacks.Remove(callback);
        }

        private static ulong GetKey(InputSources type, uint code)
        {
            return (ulong)type << 32 | code;
        }

        public static void Bind(InputSource source)
        {
            Debug.Assert(source.SourceType < InputSources.Count);
            ulong key = GetKey(source.SourceType, source.Code);
            UnBind(source.SourceType, (InputCodes)source.Code);

            if (!inputBindings.TryGetValue(source.Binding, out var value))
            {
                InputBinding inputBinding = new();
                inputBinding.Sources.Add(source);
                inputBindings[source.Binding] = inputBinding;
            }
            else
            {
                value.Sources.Add(source);
            }

            sourceBindingMap[key] = source.Binding;
        }
        public static void UnBind(InputSources type, InputCodes code)
        {
            Debug.Assert(type < InputSources.Count);
            ulong key = GetKey(type, (uint)code);
            if (!sourceBindingMap.TryGetValue(key, out ulong bindingKey))
            {
                return;
            }

            Debug.Assert(inputBindings.ContainsKey(bindingKey));
            var binding = inputBindings[bindingKey];
            uint index = uint.MaxValue;
            for (int i = 0; i < binding.Sources.Count; i++)
            {
                if (binding.Sources[i].SourceType == type && binding.Sources[i].Code == (uint)code)
                {
                    Debug.Assert(binding.Sources[i].Binding == sourceBindingMap[key]);
                    index = (uint)i;
                    break;
                }
            }

            if (index != uint.MaxValue)
            {
                binding.Sources.RemoveAt((int)index);
                sourceBindingMap.Remove(key);
            }

            if (binding.Sources.Count == 0)
            {
                Debug.Assert(!sourceBindingMap.ContainsKey(key));
                inputBindings.Remove(bindingKey);
            }
        }
        public static void UnBind(string bindingName)
        {
            ulong binding = (ulong)bindingName.GetHashCode();

            if (!inputBindings.TryGetValue(binding, out var value))
            {
                return;
            }

            foreach (var source in value.Sources)
            {
                Debug.Assert(source.Binding == binding);
                ulong key = GetKey(source.SourceType, source.Code);
                Debug.Assert(sourceBindingMap.ContainsKey(key) && sourceBindingMap[key] == binding);
                sourceBindingMap.Remove(key);
            }

            inputBindings.Remove(binding);
        }

        public static void Set(InputSources type, InputCodes code, Vector3 value)
        {
            Debug.Assert(type < InputSources.Count);
            ulong key = GetKey(type, (uint)code);
            if (!inputValues.TryGetValue(key, out var input))
            {
                input = new();
            }
            input.Previous = input.Current;
            input.Current = value;
            inputValues[key] = input;

            if (sourceBindingMap.TryGetValue(key, out ulong bindingKey))
            {
                Debug.Assert(inputBindings.ContainsKey(bindingKey));
                inputBindings[bindingKey].IsDirty = true;

                Get(bindingKey, out var bindingValue);

                // TODO: these callbacks could cause data-races in scripts when not run on the same thread as game scripts
                foreach (var c in inputCallbacks)
                {
                    c.OnEvent(bindingKey, ref bindingValue);
                }
            }

            // TODO: these callbacks could cause data-races in scripts when not run on the same thread as game scripts
            foreach (var c in inputCallbacks)
            {
                c.OnEvent(type, code, ref input);
            }
        }
        public static void Get(InputSources type, InputCodes code, out InputValue value)
        {
            Debug.Assert(type < InputSources.Count);
            ulong key = GetKey(type, (uint)code);
            inputValues.TryGetValue(key, out value);
        }
        public static void Get(string bindingName, out InputValue value)
        {
            ulong binding = (ulong)bindingName.GetHashCode();

            Get(binding, out value);
        }
        public static void Get(ulong binding, out InputValue value)
        {
            if (!inputBindings.TryGetValue(binding, out var inputBinding))
            {
                value = default;
                return;
            }

            if (!inputBinding.IsDirty)
            {
                value = inputBinding.Value;
                return;
            }

            InputValue result = new();

            foreach (var source in inputBinding.Sources)
            {
                Debug.Assert(source.Binding == binding);
                Get(source.SourceType, (InputCodes)source.Code, out var subValue);
                Debug.Assert(source.Axis <= InputAxis.Z);
                if (source.SourceType == InputSources.Mouse)
                {
                    float current = subValue.Current[(int)source.SourceAxis];
                    float previous = subValue.Previous[(int)source.SourceAxis];
                    result.Current[(int)source.Axis] += (current - previous) * source.Multiplier;
                }
                else
                {
                    result.Previous[(int)source.Axis] += subValue.Previous.X * source.Multiplier;
                    result.Current[(int)source.Axis] += subValue.Current.X * source.Multiplier;
                }
            }
            inputBinding.Value = result;
            inputBinding.IsDirty = false;

            value = result;
        }
    }
}
