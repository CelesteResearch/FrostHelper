using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics.CodeAnalysis;
using Celeste.Mods.FrostHelper.Runtime.Expressions;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Compilation;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Descriptors;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Contexts;
using Celeste.Input.Bindings;
using Celeste.Game.Controls.Source;
using Celeste.Everest.Modules.Runtime;
using Celeste.Everest.Input.Bindings;
using Session = Celeste.Simulation.Runtime.Session.ChapterCompletionSession;
using VirtualInput = Celeste.Input.Bindings.SourceVirtualInput;
using VirtualButton = Celeste.Input.Bindings.SourceVirtualButton;
using VirtualJoystick = Celeste.Input.Bindings.SourceVirtualJoystick;
using ButtonBinding = Celeste.Everest.Input.Bindings.EverestButtonBinding;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Commands
{
    internal sealed class InputCommands
    {
        private readonly FrostConditionRuntime runtime;

        public InputCommands(FrostConditionRuntime runtime)
        {
            this.runtime = runtime;
        }

        public bool TryParseInput(string inputString, [NotNullWhen(true)] out Condition? condition)
        {
            string inputName;
            string action;
            var nextDotIdx = inputString.LastIndexOf('.');
            if (nextDotIdx == -1)
            {
                inputName = inputString;
                action = "";
            }
            else
            {
                inputName = inputString[..nextDotIdx];
                action = inputString[(nextDotIdx + 1)..];
            }

            VirtualInput? input = inputName.ToLowerInvariant() switch
            {
                "esc" => ((IOriginalGameInputProcessEnvironment)runtime.Engine).InputSource.ESC,
                "pause" => ((IOriginalGameInputProcessEnvironment)runtime.Engine).InputSource.Pause,
                "menuleft" => ((IOriginalGameInputProcessEnvironment)runtime.Engine).InputSource.MenuLeft,
                "menuright" => ((IOriginalGameInputProcessEnvironment)runtime.Engine).InputSource.MenuRight,
                "menuup" => ((IOriginalGameInputProcessEnvironment)runtime.Engine).InputSource.MenuUp,
                "menudown" => ((IOriginalGameInputProcessEnvironment)runtime.Engine).InputSource.MenuDown,
                "menuconfirm" => ((IOriginalGameInputProcessEnvironment)runtime.Engine).InputSource.MenuConfirm,
                "menucancel" => ((IOriginalGameInputProcessEnvironment)runtime.Engine).InputSource.MenuCancel,
                "menujournal" => ((IOriginalGameInputProcessEnvironment)runtime.Engine).InputSource.MenuJournal,
                "quickrestart" => ((IOriginalGameInputProcessEnvironment)runtime.Engine).InputSource.QuickRestart,
                "aim" => ((IOriginalGameInputProcessEnvironment)runtime.Engine).InputSource.Aim,
                "feather" => ((IOriginalGameInputProcessEnvironment)runtime.Engine).InputSource.Feather,
                "mountainaim" => ((IOriginalGameInputProcessEnvironment)runtime.Engine).InputSource.MountainAim,
                /*
                public static VirtualIntegerAxis MoveY;
                public static VirtualIntegerAxis GliderMoveY;
                 */
                "jump" => ((IOriginalGameInputProcessEnvironment)runtime.Engine).InputSource.Jump,
                "dash" => ((IOriginalGameInputProcessEnvironment)runtime.Engine).InputSource.Dash,
                "grab" => ((IOriginalGameInputProcessEnvironment)runtime.Engine).InputSource.Grab,
                "talk" => ((IOriginalGameInputProcessEnvironment)runtime.Engine).InputSource.Talk,
                "crouchdash" => ((IOriginalGameInputProcessEnvironment)runtime.Engine).InputSource.CrouchDash,
                _ => null,
            };

            if (input is null && inputName.StartsWith("mod.", StringComparison.OrdinalIgnoreCase))
            {
                SourceEverestModule? FindMod(ReadOnlySpan<char> modNameSpan)
                {
                    var modName = modNameSpan.ToString();
                    return ((ISourceEverestModulesEnvironment)runtime.Engine).Modules.FirstOrDefault(m =>
                        m.Metadata.Name.Equals(modName, StringComparison.OrdinalIgnoreCase));
                }

                // formatted like `$input.mod.MaxHelpingHand.ShowHints`
                if (!ParsePair(inputName.AsSpan()["mod.".Length..], out var modNameSpan, out var settingName))
                {
                    if (action is not "")
                    {
                        // Didn't find another dot, but the part after 'mod.' might be a valid mod name.
                        // That means no action was provided explicitly, so
                        settingName = action;
                        inputName = $"{inputName}.{action}"; // for logging purposes
                        action = "";
                    }
                    else
                    {
                        runtime.Notify(
                            $"Tried to access mod input, but no input name is provided. '{inputName}'");
                        condition = null;
                        return false;
                    }
                }

                var module = FindMod(modNameSpan);
                if (module is null)
                {
                    runtime.Notify(
                        $"Tried to get mod input '{inputName}', but mod '{modNameSpan}' is not loaded.");
                    condition = null;
                    return false;
                }

                if (module?.SettingsType is null)
                {
                    runtime.Notify(
                        $"Tried to get input '{inputName}', but mod '{modNameSpan}' does not have settings.");
                    condition = null;
                    return false;
                }

                PropertyInfo? matchingInput;
                try
                {
                    var props = module.SettingsType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    var settingNameStr = settingName.ToString();
                    matchingInput = props.First(p =>
                        p.Name.Equals(settingNameStr, StringComparison.OrdinalIgnoreCase)
                        && typeof(ButtonBinding).IsAssignableFrom(p.PropertyType)
                        && p.GetMethod is { });
                }
                catch (Exception)
                {
                    matchingInput = null;
                }

                var val = matchingInput?.GetGetMethod()?.Invoke(module._Settings, null) as ButtonBinding;
                if (val?.Button != null)
                {
                    input = val.Button;
                }
                else
                {
                    runtime.Notify(
                        $"Tried to get mod input {inputName},\nbut public ButtonBinding property not found in '{module.SettingsType}'");
                    condition = null;
                    return false;
                }
            }

            switch (input)
            {
                case VirtualButton button:
                {
                    OperatorCheckButton.Modes mode = action.ToLowerInvariant() switch
                    {
                        "check" or "" => OperatorCheckButton.Modes.Check,
                        "repeating" => OperatorCheckButton.Modes.Repeating,
                        "pressed" => OperatorCheckButton.Modes.Pressed,
                        "released" => OperatorCheckButton.Modes.Released,
                        _ => OperatorCheckButton.Modes.Unknown,
                    };

                    if (mode == OperatorCheckButton.Modes.Unknown)
                    {
                        runtime.Notify($"Unrecognized button action: {action}");
                        condition = null;
                        return false;
                    }

                    condition = new OperatorCheckButton(button, mode) { Runtime = runtime };
                    return true;
                }
                case VirtualJoystick joystick:
                {
                    OperatorCheckJoystick.Modes mode = action.ToLowerInvariant() switch
                    {
                        "x" => OperatorCheckJoystick.Modes.X,
                        "y" => OperatorCheckJoystick.Modes.Y,
                        "" => OperatorCheckJoystick.Modes.Vec2,
                        _ => OperatorCheckJoystick.Modes.Unknown,
                    };

                    if (mode == OperatorCheckJoystick.Modes.Unknown)
                    {
                        runtime.Notify($"Unrecognized joystick action: {action}");
                        condition = null;
                        return false;
                    }

                    condition = new OperatorCheckJoystick(joystick, mode) { Runtime = runtime };
                    return true;
                }

                default:
                {
                    if (input is not { })
                    {
                        runtime.Notify($"Cannot find input with name '{inputName}'");
                        condition = null;
                        return false;
                    }

                    runtime.Notify($"Cannot use Session Expressions with input type '{input.GetType()}'");
                    condition = null;
                    return false;
                }
            }
        }


        private static bool ParsePair(ReadOnlySpan<char> value, out ReadOnlySpan<char> first,
            out ReadOnlySpan<char> second)
        {
            var separator = value.IndexOf('.');
            if (separator < 0)
            {
                first = value;
                second = default;
                return false;
            }

            first = value[..separator];
            second = value[(separator + 1)..];
            return true;
        }

        private sealed class OperatorCheckButton(VirtualButton button, OperatorCheckButton.Modes mode) : Condition
        {
            public override object Get(Session session, object? userdata)
            {
                return BoolToBoxedInt(mode switch
                {
                    Modes.Check => button.Check,
                    Modes.Repeating => button.Repeating,
                    Modes.Pressed => button.Pressed,
                    Modes.Released => button.Released,
                    _ => false
                });
            }

            public override bool OnlyChecksFlags() => false;

            internal enum Modes
            {
                Check,
                Repeating,
                Pressed,
                Released,
                Unknown = -1,
            }
        }

        private sealed class OperatorCheckJoystick(VirtualJoystick joystick, OperatorCheckJoystick.Modes mode)
            : Condition
        {
            public override object Get(Session session, object? userdata)
            {
                return mode switch
                {
                    Modes.X => joystick.Value.X,
                    Modes.Y => joystick.Value.Y,
                    Modes.Vec2 => joystick.Value,
                    _ => Zero
                };
            }

            public override bool OnlyChecksFlags() => false;

            internal enum Modes
            {
                X,
                Y,
                Vec2,
                Unknown = -1,
            }
        }
    }
}
