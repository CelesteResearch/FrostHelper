using Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions.Consts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Numerics;
using Celeste.Mods.FrostHelper.Runtime.Expressions;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Compilation;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Descriptors;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Contexts;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Commands.Binding;
using Celeste.Scene.Tracking;
using Session = Celeste.Simulation.Runtime.Session.ChapterCompletionSession;
using EntityID = Celeste.Core.Maps.MapEntityId;
using Player = Celeste.Simulation.RoomPlayerSession;
using Settings = Celeste.Game.Settings.SettingsState;
using CoreModuleSettings = Celeste.Everest.EverestCoreSettings;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Commands
{
    internal sealed partial class SimpleCommands
    {
        private readonly FrostConditionRuntime runtime;

        public SimpleCommands(FrostConditionRuntime runtime)
        {
            this.runtime = runtime;
            Registry = [];

            Register<int>("deathsHere",
                "Returns how many times the player has died in this room, resets after a screen transition.",
                new SessionFieldAccessor(nameof(Session.DeathsInCurrentLevel)));

            Register<int>("deaths",
                "Returns how many times the player has died this session.",
                new SessionFieldAccessor(nameof(Session.Deaths)));

            Register<bool>("hasGolden",
                "Returns whether the player is carrying a golden berry. 1 if they are, 0 otherwise.",
                new SessionFieldAccessor(nameof(Session.GrabbedGolden)));

            Register<bool>("restartedFromGolden",
                "Returns 1 if the current session started due to a golden death.",
                new SessionFieldAccessor(nameof(Session.RestartedFromGolden)));

            Register<int>("coreMode",
                "Returns the current core mode: 0 if not set, 1 if hot, 2 if cold.",
                new CoreModeAccessor());

            Register<bool>("photosensitive",
                "Returns whether Photosensitive Mode is enabled. 1 if it is, 0 otherwise.",
                new SettingsFieldAccessor(runtime, nameof(Settings.DisableFlashes)));

            Register<bool>("allowLightning",
                "Checks the corresponding Everest-extended Photosensitive Mode setting.",
                new CoreModuleSettingsPropertyAccessor(runtime, nameof(CoreModuleSettings.AllowLightning)));

            Register<bool>("allowScreenFlash",
                "Checks the corresponding Everest-extended Photosensitive Mode settings.",
                new CoreModuleSettingsPropertyAccessor(runtime, nameof(CoreModuleSettings.AllowScreenFlash)));

            Register<bool>("allowGlitch",
                "Checks the corresponding Everest-extended Photosensitive Mode settings.",
                new CoreModuleSettingsPropertyAccessor(runtime, nameof(CoreModuleSettings.AllowGlitch)));

            Register<bool>("allowDistort",
                "Checks the corresponding Everest-extended Photosensitive Mode settings.",
                new CoreModuleSettingsPropertyAccessor(runtime, nameof(CoreModuleSettings.AllowDistort)));

            Register<bool>("allowTextHighlight",
                "Checks the corresponding Everest-extended Photosensitive Mode settings.",
                new CoreModuleSettingsPropertyAccessor(runtime, nameof(CoreModuleSettings.AllowTextHighlight)));

            Register<int>("dashes",
                "Current player dash count.",
                new DashAccessor(runtime));

            Register<int>("maxDashes",
                "Maximum allowed player dash count.",
                new MaxDashAccessor(runtime));

            Register<float>("stamina",
                "Current player stamina.",
                new StaminaAccessor(runtime));

            Register<Player>("player",
                "Current player Entity instance.",
                new PlayerAccessor(runtime));

            Register<Vector2>("speed",
                "Player's speed.",
                new PlayerSpeedAccessor(runtime));

            Register<float>("pi",
                "The value of Pi.",
                new ConstFloat(MathF.PI));

            Register<float>("e",
                "The value of Euler's Number.",
                new ConstFloat(MathF.E));

            Register<float>("dtime",
                "The delta time between frames, taking into account Assist Mode options.",
                new DeltaTimeAccessor(runtime));

            Register<float>("time",
                "Equivalent to Scene.TimeActive in C#.",
                new TimeAccessor(runtime));

            Register<string>("roomName",
                "Gets the current room's name.",
                new SessionFieldAccessor(nameof(Session.Level)));

            Register<IEnumerable<EntityID>>("strawberries",
                "Gets the IDs of all strawberries collected this session.",
                new SessionFieldAccessor(nameof(Session.Strawberries)));

            Register<IEnumerable<string>>("flags",
                "Gets the names of all currently enabled flags.",
                new SessionFieldAccessor(nameof(Session.Flags)));
        }

        private void Register<TRet>(string name, string description, Condition condition)
            => Register<TRet>(name, [RenderPart.Default(description)], condition);

        private void Register<TRet>(string name, IReadOnlyList<RenderPart> description, Condition condition)
        {
            var desc = new CommandDescriptor
            {
                Name = name,
                Description = description,
                DeclaringType = null,
                DeclaringMod = null,
                ReturnType = TypeDescriptor.For(typeof(TRet)),
                Arguments = [],
            };
            condition.Runtime = runtime;
            condition.Descriptor ??= desc;
            Registry[name] = new SimpleCommand(desc, condition);
        }

        /// <summary>
        /// Simple commands accessible via $cmdname
        /// </summary>
        internal readonly Dictionary<string, SimpleCommand> Registry;

        // Exposed via API
        internal void RegisterSimpleCommand(string modName, string cmdName, Func<Session, object?, object> func)
        {
            var key = $"{modName}.{cmdName}";
            if (Registry.TryGetValue(key, out var existing))
            {
                ((Celeste.Everest.Diagnostics.Logging.IEverestLoggerProcessEnvironment)runtime.Engine).Logger.Warn(
                    "FrostHelper.ConditionHelper", $"Replacing simple command '${key}'");
            }

            Registry[key] = new SimpleCommand(
                new CommandDescriptor
                {
                    Name = key,
                    DeclaringMod = modName,
                    DeclaringType = null,
                    Description = [],
                    Arguments = [],
                    ReturnType = TypeDescriptor.Any,
                }, new ModApiSimpleCommand(func));
        }

        internal Condition CreateCommandFromModFunc(Func<Session, object?, object> func)
        {
            return new ModApiSimpleCommand(func);
        }

        private sealed class ModApiSimpleCommand(Func<Session, object?, object> func) : Condition
        {
            public override object Get(Session session, object? userdata)
            {
                var ret = func(session, userdata);
                if (ret is bool b)
                    return BoolToBoxedInt(b);
                return ret;
            }

            protected override IEnumerable<object> GetArgsForDebugPrint() => [func];
        }
    }
}
