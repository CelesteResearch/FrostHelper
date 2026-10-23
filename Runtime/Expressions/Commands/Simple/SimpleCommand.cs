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
    internal sealed record SimpleCommand(CommandDescriptor Descriptor, Condition Condition);
}
