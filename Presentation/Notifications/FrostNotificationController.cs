// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Diagnostics.Logging;
using Celeste.Game.Content.Dialogs;
using Celeste.Game.Execution.Startup;
using Celeste.Graphics.Rendering;
using Celeste.Scene.Entities;

namespace Celeste.Mods.FrostHelper.Presentation.Notifications
{
    [RuntimeTypeAlias("FrostHelper.Helpers.CelesteNotificationSink+NotificationController")]
    public class FrostNotificationController : SceneEntityState
    {
        public readonly List<FrostNotification> Notifications = new();
        private readonly ISourceEngineContext engine;
        private readonly OriginalGameTextProcess text;
        private readonly Func<float, float> expoInOut;
        private readonly ISourceExtremaArithmetic extrema;

        internal FrostNotificationController(ISourceEngineContext engine)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
        {
            this.engine = engine;
            text = ((IOriginalGameTextEnvironment)engine).Text;
            expoInOut = ((ISourceNamedEasingArithmetic)((ISourceEasingEnvironment)engine).EasingArithmetic)
                .NamedEasers["ExpoInOut"];
            extrema = ((ISourceExtremaEnvironment)engine).ExtremaArithmetic;
            Tags = ((IOriginalGameTagsEnvironment)engine).Tags.HUD.Value;
        }

        public void Push(FrostNotification notification)
        {
            notification.Time = DateTime.Now;
            notification.Alpha += .25f * Notifications.Count;
            Notifications.Add(notification);
            ((IEverestLoggerProcessEnvironment)engine).Logger.Log(notification.Level, "FrostHelper/Notifications",
                notification.Message);
        }

        public override void Render(ISourceRenderContext context)
        {
            base.Render(context);
            float y = 1065f;
            for (int i = Notifications.Count - 1; i >= 0; i--)
            {
                var notification = Notifications[i];
                notification.Alpha -= engine.DeltaTime / 8f;
                Notifications[i] = notification;
                float alpha = expoInOut(extrema.Min(1f, notification.Alpha * 2.35f));
                Vector2 scale = Vector2.One / 3f * 2f;
                Vector2 size = text.ActiveFont.Measure(notification.Message) * scale;
                float width = size.X + 30f;
                float height = extrema.Max(size.Y + 30f, 100f);
                Vector2 position = new(1920f - width - 15f, y - height);
                PackedColor gray = new(38, 38, 38);
                SourcePrimitiveDrawing.Rect(context, this, position.X, position.Y, width, height,
                    gray * .75f * alpha);
                SourcePrimitiveDrawing.HollowRect(context, this, position.X, position.Y, width, height, gray * alpha);
                text.ActiveFont.Draw(context, this, notification.Message, position + new Vector2(15f, 0f),
                    Vector2.Zero, scale, PackedColor.White * alpha);
                Vector2 timestampPosition = position + new Vector2(15f, height - 20f);
                string timestamp = notification.Time.ToString(CultureInfo.CurrentCulture);
                Vector2 smallScale = Vector2.One / 4f;
                text.ActiveFont.Draw(context, this, timestamp, timestampPosition, Vector2.Zero, smallScale,
                    PackedColor.White * alpha);
                if (notification.Source != null)
                    text.ActiveFont.Draw(context, this, notification.Source,
                        timestampPosition + new Vector2(text.ActiveFont.Measure(timestamp).X * smallScale.X, 0f),
                        Vector2.Zero, smallScale, PackedColor.White * alpha);
                y -= height + 15f;
            }

            Notifications.RemoveAll(static notification => notification.Alpha <= 0f);
        }
    }
}
