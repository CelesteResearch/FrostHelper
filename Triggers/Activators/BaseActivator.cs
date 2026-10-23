#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Scene.Entities;
using Celeste.Scene.Scheduling;
using Celeste.Scene.Tracking;
using Celeste.Physics.Collision.Shapes;
using Celeste.Mods.FrostHelper.Runtime.Activators;
using Trigger = Celeste.Simulation.Gameplay.Triggers.Foundation.SourceTriggerEntityState;
using Player = Celeste.Simulation.RoomPlayerSession;
using Hitbox = Celeste.Physics.Collision.Shapes.HitboxColliderGeometry;

namespace Celeste.Mods.FrostHelper.Triggers.Activators
{
    [RuntimeTypeAlias("FrostHelper.Triggers.Activator.BaseActivator")]
    public partial class BaseActivator : Trigger
    {
        private readonly ISourceEngineContext engine;
        private readonly FrostActivatorServices activators;
        public Vector2[] Nodes;
        public readonly bool OnlyOnce;
        public readonly float Delay;
        public bool ActivateAfterDeath;

        private readonly bool _scanEachActivation;


        internal (List<Trigger> main, List<Trigger> elseBranch)? ToActivate;

        internal List<Trigger>?[]? ToActivatePerNode;
        internal List<Trigger>?[]? ToActivateElsePerNode;

        internal virtual bool NeedsNodeIndexes => false;

        // used by Random and Cycle modes to keep track of which trigger to call OnStay for.
        // For the 'all' setting, all triggers get activated instead (though this value is still used to keep track of whether anything got activated or not)
        private Trigger? lastActivatedTrigger;

        public ActivationModes ActivationMode;
        private int _cycleModeIdx;

        public enum ActivationModes
        {
            AllOrdered, // all triggers get activated at once, ordered by node id. Map editor default.
            CycleCorrect, // each time this activator gets triggered, the next trigger gets activated, wrapping over to the first one once all other ones have been triggered.
            Cycle, // CycleCorrect, but orders in the opposite direction, so the last node gets activated first...
            Random, // a random (seeded) trigger will be chosen.
            All, // all triggers get activated at once - deprecated
        }

        public BaseActivator(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration,
                data.Position + offset, data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            this.engine = engine;
            activators = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<IFrostActivatorEnvironment>()
                .Single().Activators;
            Nodes = data.NodesOffset(offset);
            OnlyOnce = data.Bool("once");
            Delay = data.Float("delay", 0f);
            ActivationMode = data.Enum("activationMode", ActivationModes.All);
            ActivateAfterDeath = data.Bool("activateAfterDeath", false);
            _scanEachActivation = data.Bool("scanEachActivation");
        }

        internal (List<Trigger> main, List<Trigger> elseBranch) ScanToActivateIfNeeded()
        {
            if (_scanEachActivation)
            {
                return (ToActivate = FastCollideAll()).Value;
            }

            return ToActivate ??= FastCollideAll();
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);

            if (!_scanEachActivation)
                ScanToActivateIfNeeded();
        }

        public void ActivateAll(Player player)
        {
            if (Delay == 0)
            {
                InstantActivateAll(player);
            }
            else
            {
                Add(new SimulationCoroutine(engine, DelayedActivateAll(player)));
            }

            if (OnlyOnce)
                RemoveSelf();
        }

        public void ActiveElseBlocks(Player player)
        {
            if (Delay == 0)
            {
                InstantActivateAll(player, activateElseBranch: true);
            }
            else
            {
                Add(new SimulationCoroutine(engine, DelayedActivateAll(player, activateElseBranch: true)));
            }

            if (OnlyOnce)
                RemoveSelf();
        }

        public void ActivateAtNode(Player player, int nodeIdx)
        {
            ScanToActivateIfNeeded();

            if (ToActivatePerNode is null)
                throw new Exception(
                    $"ActivateAtNode called for an Activator [{GetType()}] which doesn't keep track of nodes. This is a Frost Helper bug!");

            var toActivate = ToActivatePerNode.ElementAtOrDefault(nodeIdx);

            if (toActivate is { Count: > 0 })
            {
                if (Delay == 0)
                {
                    InstantActivateAll(player, toActivate);
                }
                else
                {
                    Add(new SimulationCoroutine(engine, DelayedActivateAll(player, toActivate)));
                }
            }

            if (OnlyOnce)
                RemoveSelf();
        }

        /// <summary>
        /// Calls the OnStay method on all just activated triggers.
        /// </summary>
        /// <param name="player"></param>
        // The installed FrostHelper body intentionally disables continued OnStay/OnLeave delivery.
        public void CallOnStay(Player? player = null)
        {
        }

        public void CallOnLeave(Player? player = null)
        {
        }

        private IEnumerator DelayedActivateAll(Player player, bool activateElseBranch = false)
        {
            yield return Delay;
            InstantActivateAll(player, activateElseBranch);
        }

        private IEnumerator DelayedActivateAll(Player player, List<Trigger> toActivate)
        {
            yield return Delay;
            InstantActivateAll(player, toActivate);
        }

        public void InstantActivateAll(Player player, bool activateElseBranch = false)
        {
            // There's a chance for an activator to get triggered *before* Awake.
            ScanToActivateIfNeeded();

            InstantActivateAll(player, activateElseBranch ? ToActivate!.Value.elseBranch : ToActivate!.Value.main);
        }

        internal void InstantActivateAll(Player player, List<Trigger> toActivate)
        {
            if (toActivate.Count == 0 || ((player?.Scene is null) && !ActivateAfterDeath))
                return;
            CallOnLeave(player);
            switch (ActivationMode)
            {
                case ActivationModes.All:
                    foreach (var trigger in toActivate)
                    {
                        Activate(player!, trigger);
                    }

                    break;
                case ActivationModes.AllOrdered:
                    foreach (var trigger in toActivate)
                    {
                        Activate(player!, trigger);
                    }

                    break;
                case ActivationModes.Cycle or ActivationModes.CycleCorrect:
                    Activate(player!, toActivate[_cycleModeIdx]);
                    _cycleModeIdx = (_cycleModeIdx + 1) % toActivate.Count;
                    break;
                case ActivationModes.Random:
                    Activate(player!, toActivate[((ISourceRandomEnvironment)engine).Random.Next(0, toActivate.Count)]);
                    break;
            }
        }


        private void Activate(Player player, Trigger trigger)
        {
            if (trigger.Scene is null)
                return;

            if (trigger.PlayerIsInside)
            {
                trigger.OnLeave(player);
            }

            trigger.OnEnter(player);
            // Some triggers only do their thing in OnStay, so let's call that as well

            trigger.OnStay(player);

            activators.InvokeActivated(this, trigger, player);
            lastActivatedTrigger = trigger;
        }

        /// <summary>
        /// Struct containing a <typeparamref name="T"/> and an int index. Used because value tuples don't exist in framework :(
        /// </summary>
        /// <typeparam name="T"></typeparam>
        internal struct Indexed<T>
        {
            public T Value;
            public int Index;
        }
    }
}
