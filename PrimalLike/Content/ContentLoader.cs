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
        delegate bool ComponentReader(BinaryReader reader, ref EntityInfo info);
        static readonly ComponentReader[] componentReaders =
        [
            ReadTransform,
            ReadScript,
            ReadGeometry,
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

                    if (!componentReaders[componentType](reader, ref info))
                    {
                        return false;
                    }
                }

                var entity = GameEntity.Create(info);
                if (!entity.IsValid)
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

        private static bool ReadTransform(BinaryReader reader, ref EntityInfo info)
        {
            int componentSize = reader.ReadInt32();
            long position = reader.BaseStream.Position;

            TransformInfo transform = new()
            {
                Position = new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                Rotation = Quaternion.CreateFromYawPitchRoll(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                Scale = new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())
            };

            Debug.Assert(position + componentSize == reader.BaseStream.Position);

            info.Transform = transform;

            return true;
        }
        private static bool ReadScript(BinaryReader reader, ref EntityInfo info)
        {
            int componentSize = reader.ReadInt32();
            long position = reader.BaseStream.Position;

            string scriptName = Encoding.UTF8.GetString(reader.ReadBytes(componentSize));

            ScriptInfo script = new()
            {
                ScriptCreator = Script.GetScriptCreator(scriptName)
            };

            Debug.Assert(position + componentSize == reader.BaseStream.Position);

            info.Script = script;

            return true;
        }
        private static bool ReadGeometry(BinaryReader reader, ref EntityInfo info)
        {
            return false;
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
