using System;
using System.Reflection;
using System.Reflection.Emit;
using Celeste.Game.Gameplay.Enemies.Badeline.Chasers;

namespace Celeste.Mods.FrostHelper.Runtime.BadelineChasers
{
    internal static class BadelineChaserFields
    {
        internal static readonly Func<BadelineOldsiteState, bool> Following = Bind<bool>("following");
        internal static readonly Func<BadelineOldsiteState, float> FollowBehindTime = Bind<float>("followBehindTime");

        internal static readonly Func<BadelineOldsiteState, float> FollowBehindIndexDelay =
            Bind<float>("followBehindIndexDelay");

        internal static void Prepare() => _ = Following;

        private static Func<BadelineOldsiteState, T> Bind<T>(string name)
        {
            var field = typeof(BadelineOldsiteState).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            var method = new DynamicMethod("FrostHelper.BadelineChaser." + name, typeof(T),
                new[] { typeof(BadelineOldsiteState) }, typeof(BadelineChaserFields), true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Ret);
            return (Func<BadelineOldsiteState, T>)method.CreateDelegate(typeof(Func<BadelineOldsiteState, T>));
        }
    }
}
