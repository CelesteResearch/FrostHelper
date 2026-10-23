using Celeste.Core.Reflection.Interop;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Persistence;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Everest;
using Celeste.Everest.Modules.Runtime;
using Celeste.Everest.Diagnostics.Logging;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Settings;
using Celeste.Mods.FrostHelper.Color;
using Celeste.Mods.FrostHelper.Presentation.Notifications;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Commands;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Commands.Types;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Contexts;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Operators;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Syntax;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions
{
    public sealed partial class FrostConditionRuntime
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, FrostConditionRuntime> Owners = new();

        public static FrostConditionRuntime For(ISourceEngineContext engine) =>
            Owners.GetValue(engine, static e => new FrostConditionRuntime(e));

        internal static void Release(ISourceEngineContext engine)
        {
            if (Owners.TryGetValue(engine, out var owner))
            {
                owner.entityTypeNames?.Dispose();
                owner.savestateLease?.Dispose();
            }

            Owners.Remove(engine);
        }

        private IDisposable savestateLease;
        private ExpressionSavestateImports savestateImports;
        internal void Load() => _ = EntityTypeNames;

        internal void LoadContent()
        {
            if (savestateImports is null)
            {
                savestateImports = new ExpressionSavestateImports();
                savestateLease = ((ISourceModInteropEnvironment)Engine).ModInterop.RegisterOwned(
                    new SourceModInteropActualTypeBinding(typeof(ExpressionSavestateImports), savestateImports));
            }

            savestateImports.AddReturnSameObjectProcessor?.Invoke(static type =>
                typeof(ISavestatePersisted).IsAssignableFrom(type));
        }

        public ISourceEngineContext Engine { get; }
        public object CurrentScene => Engine.Frame.Scenes.CurrentScene;
        public OriginalLevelSceneRoot CurrentLevel => (OriginalLevelSceneRoot)CurrentScene;
        public SettingsState Settings => ((ISettingsRuntimeContext)Engine).SettingsProcess.Instance;
        public EverestCoreSettings CoreModuleSettings => ((IEverestCoreSettingsEnvironment)Engine).CurrentCoreSettings;
        private ColorReader colors;
        internal ColorReader Colors => colors ??= new ColorReader(Notify);
        internal ExpressionParser Parser { get; }
        private SimpleCommands simpleCommands;
        private FunctionCommands functionCommands;
        private FieldAccessCommands fieldAccessCommands;
        private InstanceFunctionCommands instanceFunctionCommands;
        private InputCommands inputCommands;
        private MathOperatorRegistry mathOperators;
        internal SimpleCommands SimpleCommands => simpleCommands ??= new SimpleCommands(this);
        internal FunctionCommands FunctionCommands => functionCommands ??= new FunctionCommands(this);
        internal FieldAccessCommands FieldAccessCommands => fieldAccessCommands ??= new FieldAccessCommands(this);

        internal InstanceFunctionCommands InstanceFunctionCommands =>
            instanceFunctionCommands ??= new InstanceFunctionCommands(this);

        internal InputCommands InputCommands => inputCommands ??= new InputCommands(this);
        internal MathOperatorRegistry MathOperators => mathOperators ??= new MathOperatorRegistry(this);
        private EntityTypeNames entityTypeNames;

        internal EntityTypeNames EntityTypeNames
        {
            get
            {
                if (entityTypeNames != null) return entityTypeNames;
                entityTypeNames = new EntityTypeNames(this);
                entityTypeNames.Load();
                return entityTypeNames;
            }
        }

        public IExpressionContext Context { get; }

        private FrostConditionRuntime(ISourceEngineContext engine)
        {
            Engine = engine;
            EmptyCondition.Runtime = this;
            TrueCondition.Runtime = this;
            FalseCondition.Runtime = this;
            Parser = new ExpressionParser(new ExpressionErrorLogger(Notify));
            Context = new ExpressionContext(this, new(), new());
        }

        public void Notify(string message) => FrostNotificationSink.For(Engine)
            .Push(new FrostNotification(EverestLogLevel.Error, message));

        internal void LogCompilationFailure(string message) =>
            ((IEverestLoggerProcessEnvironment)Engine).Logger.Error("FrostHelper.CompiledCondition", message);
    }
}
