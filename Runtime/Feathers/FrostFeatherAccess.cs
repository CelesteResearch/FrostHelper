using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Numerics;
using Celeste.Simulation;
using Celeste.Game.Execution.Levels;

namespace Celeste.Mods.FrostHelper.Runtime.Feathers
{
    internal static class FrostFeatherAccess
    {
        internal static readonly Action<RoomPlayerSession, int, int, string, Vector2> Rumble =
            (Action<RoomPlayerSession, int, int, string, Vector2>)typeof(RoomPlayerSession)
                .GetMethod("ReportRumble", BindingFlags.Instance | BindingFlags.NonPublic)
                .CreateDelegate(typeof(Action<RoomPlayerSession, int, int, string, Vector2>));

        internal static void Displace(RoomPlayerSession player) =>
            ((OriginalLevelSceneRoot)player.Scene).Displacement.AddBurst(player.Center, .25f, 8f, 32f, 1f);

        internal static readonly Action<RoomPlayerSession, float> SetSpeedLerp =
            PropertySetter<float>("StarFlySpeedLerp");

        internal static readonly
            Action<RoomPlayerSession, Celeste.Simulation.Presentation.PostProcessing.Bloom.BloomPointState> SetBloom =
                PropertySetter<Celeste.Simulation.Presentation.PostProcessing.Bloom.BloomPointState>("StarFlyBloom");

        internal static readonly Action<RoomPlayerSession, Celeste.Simulation.Audio.Sources.SoundSourceComponentState>
            SetLoopSound =
                PropertySetter<Celeste.Simulation.Audio.Sources.SoundSourceComponentState>("StarFlyLoopSound");

        internal static readonly Action<RoomPlayerSession, Celeste.Simulation.Audio.Sources.SoundSourceComponentState>
            SetWarningSound =
                PropertySetter<Celeste.Simulation.Audio.Sources.SoundSourceComponentState>("StarFlyWarningSound");

        internal static readonly Action<RoomPlayerSession, bool> SetTransforming =
            PropertySetter<bool>("StarFlyTransforming");

        internal static readonly Action<RoomPlayerSession, Vector2> SetLastDirection =
            PropertySetter<Vector2>("StarFlyLastDirection");

        internal static Action<RoomPlayerSession, T> FieldSetter<T>(string name)
        {
            var method = new DynamicMethod("FrostFeather.Set." + name, typeof(void),
                new[] { typeof(RoomPlayerSession), typeof(T) }, typeof(RoomPlayerSession), true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld,
                typeof(RoomPlayerSession).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic));
            il.Emit(OpCodes.Ret);
            return (Action<RoomPlayerSession, T>)method.CreateDelegate(typeof(Action<RoomPlayerSession, T>));
        }

        private static Action<RoomPlayerSession, T> PropertySetter<T>(string name) =>
            (Action<RoomPlayerSession, T>)typeof(RoomPlayerSession).GetProperty(name).SetMethod
                .CreateDelegate(typeof(Action<RoomPlayerSession, T>));
    }
}
