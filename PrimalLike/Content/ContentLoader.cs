using PrimalLike.Components;
using PrimalLike.EngineAPI;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Text;

namespace PrimalLike.Content
{
    static class ContentLoader
    {
        enum ComponentType
        {
            Transform,
            Script,
        }

        delegate bool ComponentReader(byte[] data, ref EntityInfo info);
        static readonly ComponentReader[] componentReaders =
        [
            ReadTransform,
            ReadScript,
        ];

        static readonly List<Entity> entities = [];

        public static bool LoadGame(string path)
        {
            using FileStream fileStream = new(path, FileMode.Open, FileAccess.Read);
            using BinaryReader reader = new(fileStream, Encoding.UTF8, false);

            int numEntities = reader.ReadInt32();
            Debug.Assert(numEntities > 0, "The number of entities must be greater than zero.");
            for (int i = 0; i < numEntities; i++)
            {
                EntityInfo info = new();

                reader.ReadInt32(); // Reserved for future use
                int numComponents = reader.ReadInt32();
                if (numComponents <= 0)
                {
                    return false;
                }

                for (int j = 0; j < numComponents; j++)
                {
                    int componentType = reader.ReadInt32();
                    Debug.Assert(componentType < componentReaders.Length);

                    int componentSize = reader.ReadInt32();
                    byte[] componentData = reader.ReadBytes(componentSize);
                    if (!componentReaders[componentType](componentData, ref info))
                    {
                        return false;
                    }
                }

                var entity = GameEntity.Create(info);
                if (!entity.IsValid())
                {
                    return false;
                }
                entities.Add(entity);
            }

            return true;
        }

        public static void UnloadGame()
        {
            foreach (var entity in entities)
            {
                GameEntity.Remove(entity.Id);
            }
        }

        private static bool ReadTransform(byte[] data, ref EntityInfo info)
        {
            using MemoryStream stream = new(data);
            using BinaryReader reader = new(stream, Encoding.UTF8, false);

            TransformInfo transform = new()
            {
                Position = new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                Rotation = Quaternion.CreateFromYawPitchRoll(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                Scale = new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())
            };

            info.Transform = transform;

            return true;
        }

        private static bool ReadScript(byte[] data, ref EntityInfo info)
        {
            string scriptName = Encoding.UTF8.GetString(data);

            ScriptInfo script = new()
            {
                ScriptCreator = Script.GetScriptCreator(scriptName)
            };

            info.Script = script;

            return true;
        }

        public static bool LoadEngineShaders(string path, out byte[] shaders)
        {
            return ReadFile(path, out shaders);
        }

        private static bool ReadFile(string path, out byte[] data)
        {
            data = null;
            if (!File.Exists(path))
            {
                return false;
            }

            data = File.ReadAllBytes(path);
            return data.Length > 0;
        }
    }
}
