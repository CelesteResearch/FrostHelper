using System.Numerics;
using Celeste.Mods.FrostHelper.Entities.Collectibles.Feathers;
using Celeste.Simulation.Audio.Sources;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;

namespace Celeste.Mods.FrostHelper.Runtime.Feathers
{
    public sealed partial class FrostFeatherPlayerState
    {
        private void Begin(CustomFeather feather)
        {
            runtime.Load();
            player.Sprite.Play("startStarFly");
            FrostFeatherAccess.SetTransforming(player, true);
            player.Data.StarFlyTimer = feather.FlyTime;
            FrostFeatherAccess.SetSpeedLerp(player, 0);
            player.Data.Motion.JumpGraceTimer = 0;
            var bloom = player.StarFlyBloom;
            if (bloom == null)
            {
                bloom = new BloomPointState { Position = new Vector2(0, -6), Alpha = 0, Radius = 16 };
                player.Components.Add(bloom);
            }

            bloom.Visible = true;
            bloom.Alpha = 0;
            FrostFeatherAccess.SetBloom(player, bloom);
            player.World.Contacts.SetCollider(player.World.Contacts.FeatherHitbox);
            player.World.Contacts.Hurtbox = player.World.Contacts.FeatherHurtbox;
            var loop = player.StarFlyLoopSound;
            var warning = player.StarFlyWarningSound;
            if (loop == null)
            {
                var sources = ((ISourceSoundSourceEnvironment)runtime.Engine).SoundSources;
                loop = sources.Create();
                player.Components.Add(loop);
                loop.DisposeOnTransition = false;
                warning = sources.Create();
                player.Components.Add(warning);
                warning.DisposeOnTransition = false;
            }

            loop.Play("event:/game/06_reflection/feather_state_loop", "feather_speed", 1);
            if (warning != null) warning.Stop();
            FrostFeatherAccess.SetLoopSound(player, loop);
            FrostFeatherAccess.SetWarningSound(player, warning);
        }
    }
}
