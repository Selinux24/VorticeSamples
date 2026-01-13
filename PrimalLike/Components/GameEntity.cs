global using EntityId = uint;
using PrimalLike.Common;
using PrimalLike.EngineAPI;
using System.Collections.Generic;
using System.Diagnostics;

namespace PrimalLike.Components
{
    public static class GameEntity
    {
        static readonly List<GenerationType> generations = [];
        static readonly Queue<EntityId> freeIds = [];

        static readonly List<TransformComponent> transforms = [];
        static readonly List<ScriptComponent> scripts = [];
        static readonly List<GeometryComponent> geometries = [];

        public static TransformComponent GetTransform(EntityId index)
        {
            return transforms[(int)index];
        }
        public static ScriptComponent GetScript(EntityId index)
        {
            return scripts[(int)index];
        }
        public static GeometryComponent GetGeometry(EntityId index)
        {
            return geometries[(int)index];
        }

        public static Entity Create(EntityInfo info)
        {
            EntityId id;
            if (freeIds.Count > IdDetail.MinDeletedElements)
            {
                id = freeIds.Dequeue();
                Debug.Assert(!IsAlive(id));
                id = IdDetail.NewGeneration(id);
                ++generations[(int)IdDetail.Index(id)];
            }
            else
            {
                id = (EntityId)generations.Count;
                generations.Add(0);
                transforms.Add(new());
                scripts.Add(new());
                geometries.Add(new());
            }

            Entity entity = new(id);
            IdType index = IdDetail.Index(id);

            Debug.Assert(!transforms[(int)index].IsValid());
            transforms[(int)index] = Transform.Create(info.Transform, entity);
            Debug.Assert(transforms[(int)index].IsValid());

            if (info.Script != null && info.Script?.ScriptCreator != null)
            {
                Debug.Assert(!scripts[(int)index].IsValid());
                scripts[(int)index] = Script.Create(info.Script.Value, entity);
                Debug.Assert(scripts[(int)index].IsValid());
            }

            if (info.Geometry != null)
            {
                Debug.Assert(!geometries[(int)index].IsValid());
                geometries[(int)index] = Geometry.Create(info.Geometry.Value, entity);
                Debug.Assert(geometries[(int)index].IsValid());
            }

            return entity;
        }
        public static void Remove(EntityId id)
        {
            IdType index = IdDetail.Index(id);
            Debug.Assert(IsAlive(id));

            if (geometries[(int)index].IsValid())
            {
                Geometry.Remove(geometries[(int)index]);
                geometries[(int)index] = new();
            }

            if (scripts[(int)index].IsValid())
            {
                Script.Remove(scripts[(int)index]);
                scripts[(int)index] = new();
            }

            if (transforms[(int)index].IsValid())
            {
                Transform.Remove(transforms[(int)index]);
                transforms[(int)index] = new();
            }

            if (generations[(int)index] < IdDetail.MaxGeneration)
            {
                freeIds.Enqueue(id);
            }
        }
        public static bool UpdateComponent(EntityId id, EntityInfo info, ComponentTypes type)
        {
            Debug.Assert(IsAlive(id) && type != ComponentTypes.Transform);
            if (type == ComponentTypes.Transform) return false;
            Entity entity = new(id);
            IdType index = IdDetail.Index(id);

            if (type == ComponentTypes.Script)
            {
                if (scripts[(int)index].IsValid())
                {
                    Script.Remove(scripts[(int)index]);
                    scripts[(int)index] = new();
                }

                if (info.Script?.ScriptCreator != null)
                {
                    ScriptComponent newScript = Script.Create(info.Script.Value, entity);
                    Debug.Assert(newScript.IsValid());

                    if (newScript.IsValid())
                    {
                        scripts[(int)index] = newScript;
                        return true;
                    }
                }
            }
            else if (type == ComponentTypes.Geometry)
            {
                if (geometries[(int)index].IsValid())
                {
                    Geometry.Remove(geometries[(int)index]);
                    geometries[(int)index] = new();
                }

                if (info.Geometry != null)
                {
                    GeometryComponent newGeometry = Geometry.Create(info.Geometry.Value, entity);
                    Debug.Assert(newGeometry.IsValid());

                    if (newGeometry.IsValid())
                    {
                        geometries[(int)index] = newGeometry;
                        return true;
                    }
                }
            }

            return false;
        }
        public static bool IsAlive(EntityId id)
        {
            Debug.Assert(IdDetail.IsValid(id), "The entity id is not valid.");
            IdType index = IdDetail.Index(id);
            Debug.Assert(index < generations.Count, "Index is outside of the generation list bounds.");
            return generations[(int)index] ==
                IdDetail.Generation(id) &&
                transforms[(int)index].IsValid();
        }
    }
}
