#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Scene.Tracking;
using Trigger = Celeste.Simulation.Gameplay.Triggers.Foundation.SourceTriggerEntityState;
using Hitbox = Celeste.Physics.Collision.Shapes.HitboxColliderGeometry;

namespace Celeste.Mods.FrostHelper.Triggers.Activators
{
    public partial class BaseActivator
    {
        internal (List<Trigger> main, List<Trigger> elseBranch) FastCollideAll()
        {
            static void Add(Trigger entity, int i, List<Trigger>? into, List<Trigger>? intoElse,
                List<Indexed<Trigger>>? intoWithIndexes, List<Indexed<Trigger>>? intoElseWithIndexes)
            {
                if (entity is IIfActivator { IsElse: true })
                {
                    if (intoElse is { })
                        intoElse.Add(entity);
                    else
                        intoElseWithIndexes!.Add(new()
                        {
                            Value = entity,
                            Index = i,
                        });
                }
                else
                {
                    if (into is { })
                        into.Add(entity);
                    else
                        intoWithIndexes!.Add(new()
                        {
                            Value = entity,
                            Index = i,
                        });
                }
            }

            // When we're cycling, the order of triggered triggers should be decided by node order, so that it's manipulatable easily.
            // In all other modes, order is not important, so we don't sort at all for better performance
            // Since the index is unnecessary on non-cycle modes, we'll create a List<T> directly, instead of going through Indexed<T> to then re-allocate it into List<T>.
            // TODO: maybe rewrite into storing the indexes separately, in a stack-allocated buffer??
            List<Trigger>? into = null;
            List<Trigger>? intoElse = null;

            List<Indexed<Trigger>>? intoWithIndexes = null;
            List<Indexed<Trigger>>? intoElseWithIndexes = null;
            if (ActivationMode is ActivationModes.Cycle or ActivationModes.CycleCorrect or ActivationModes.AllOrdered ||
                NeedsNodeIndexes)
            {
                intoWithIndexes = [];
                intoElseWithIndexes = [];
            }
            else
            {
                into = [];
                intoElse = [];
            }

            var nodes = Nodes;
            var maxNodeId = 0;

            foreach (object tracked in ((ISourceScene)Scene).SourceScene.Entities.GetEntitiesTrackIfNeeded<Trigger>())
            {
                var entity = (Trigger)tracked;
                var ePos = entity.Position;

                switch (entity.Collider)
                {
                    case Hitbox eCol:
                    {
                        // Fast path for the 99.99% of triggers that use Hitbox colliders
                        var eRight = ePos.X + eCol.Width;
                        var eBottom = ePos.Y + eCol.Height;

                        for (int i = 0; i < nodes.Length; i++)
                        {
                            Vector2 node = nodes[i];
                            if (node.X < eRight
                                && node.X > ePos.X
                                && node.Y < eBottom
                                && node.Y > ePos.Y)
                            {
                                maxNodeId = Math.Max(maxNodeId, i);
                                Add(entity, i, into, intoElse, intoWithIndexes, intoElseWithIndexes);
                                break;
                            }
                        }

                        break;
                    }
                    case { } otherCollider:
                    {
                        for (int i = 0; i < nodes.Length; i++)
                        {
                            Vector2 node = nodes[i];
                            if (otherCollider.Collide(node))
                            {
                                maxNodeId = Math.Max(maxNodeId, i);
                                Add(entity, i, into, intoElse, intoWithIndexes, intoElseWithIndexes);
                                break;
                            }
                        }

                        break;
                    }
                }
            }

            // If we have kept track of indexes, then we need to sort
            if (intoWithIndexes is { })
            {
                if (ActivationMode is ActivationModes.Cycle)
                {
                    // backwards compat: old 'Cycle' mode sorted in the wrong order:
                    intoWithIndexes.Sort((p1, p2) => p2.Index - p1.Index);
                    intoElseWithIndexes!.Sort((p1, p2) => p2.Index - p1.Index);
                }
                else
                {
                    intoWithIndexes.Sort((p1, p2) => p1.Index - p2.Index);
                    intoElseWithIndexes!.Sort((p1, p2) => p1.Index - p2.Index);
                }


                if (NeedsNodeIndexes)
                {
                    ToActivatePerNode = new List<Trigger>?[maxNodeId + 1];
                    ToActivateElsePerNode = new List<Trigger>?[maxNodeId + 1];
                    foreach (var item in intoWithIndexes)
                    {
                        ToActivatePerNode[item.Index] ??= new(1);
                        ToActivatePerNode[item.Index]!.Add(item.Value);
                    }

                    foreach (var item in intoElseWithIndexes)
                    {
                        ToActivateElsePerNode[item.Index] ??= new(1);
                        ToActivateElsePerNode[item.Index]!.Add(item.Value);
                    }
                }

                // Now, convert our list to just a List<T>.
                // Done manually for performance and less allocations.
                into = new(intoWithIndexes.Count);
                intoElse = new(intoElseWithIndexes.Count);
                foreach (var item in intoWithIndexes)
                {
                    into.Add(item.Value);
                }

                foreach (var item in intoElseWithIndexes)
                {
                    intoElse.Add(item.Value);
                }
            }

            return (into!, intoElse!);
        }
    }
}
