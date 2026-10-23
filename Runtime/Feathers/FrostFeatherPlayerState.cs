using System;
using System.Collections;
using System.Numerics;
using System.Reflection;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Mods.FrostHelper.Entities.Collectibles.Feathers;
using Celeste.Simulation;

namespace Celeste.Mods.FrostHelper.Runtime.Feathers
{
    public sealed partial class FrostFeatherPlayerState
    {
        private readonly FrostFeatherRuntime runtime;
        private readonly RoomPlayerSession player;
        public int StateId { get; }
        public CustomFeather CurrentFeather { get; private set; }

        private static readonly Action<RoomPlayerSession, PackedColor> SetFlyColor =
            FrostFeatherAccess.FieldSetter<PackedColor>("starFlyColor");

        internal FrostFeatherPlayerState(FrostFeatherRuntime runtime, RoomPlayerSession player)
        {
            this.runtime = runtime;
            this.player = player;
            StateId = ((ISourceNamedStateMachine)player.Machine).AddState("Custom Feather", Update, Coroutine, Begin,
                End);
        }

        public bool Start(CustomFeather feather)
        {
            if (feather.RefillStamina) player.RefillStamina();
            if (player.Machine.State == 18) return false;
            CurrentFeather = feather;
            if (player.Machine.State == StateId)
            {
                player.Data.StarFlyTimer = feather.FlyTime;
                player.Sprite.Color = feather.FlyColor;
                FrostFeatherAccess.Rumble(player, 1, 1, "MediumMedium", player.Body.Position);
            }
            else player.Machine.State = StateId;

            return true;
        }

        public void ApplyFlyColor(PackedColor color) => SetFlyColor(player, color);

        private void Begin()
        {
            var feather = CurrentFeather;
            if (feather != null) Begin(feather);
        }

        private void End()
        {
            var feather = CurrentFeather;
            if (feather != null) runtime.Methods.End(player, feather);
        }

        private int Update()
        {
            var level = (Celeste.Game.Execution.Levels.OriginalLevelSceneRoot)player.Scene;
            var bloom = player.StarFlyBloom;
            var feather = CurrentFeather;
            return feather == null ? 0 : runtime.Methods.Update(player, feather, StateId, level, bloom);
        }
    }
}
