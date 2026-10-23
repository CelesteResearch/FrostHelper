using System;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Simulation;

namespace Celeste.Mods.FrostHelper.Runtime.Springs
{
    internal readonly struct SpringRecovery
    {
        internal readonly struct SavedPlayerData
        {
            internal readonly int PrevDashes;
            internal readonly float PrevStamina;

            internal SavedPlayerData(RoomPlayerSession player)
            {
                PrevDashes = player.Data.Dashes;
                PrevStamina = player.Data.Stamina;
            }
        }

        private readonly int dashes, stamina;

        private SpringRecovery(int dashes, int stamina)
        {
            this.dashes = dashes;
            this.stamina = stamina;
        }

        internal static SpringRecovery Read(EntityData data)
        {
            int dash = EntityAttributeReader.Int(data.Values, "dashRecovery", 10000),
                sta = EntityAttributeReader.Int(data.Values, "staminaRecovery", 10000),
                jump = EntityAttributeReader.Int(data.Values, "jumpRecovery", 10001);
            if (data.Values.ContainsKey("recovery"))
            {
                var p = EntityAttributeReader.Attr(data.Values, "recovery", "").Split(';');
                if (p.Length != 3) throw new FormatException("Invalid spring recovery.");
                dash = int.Parse(p[0]);
                sta = int.Parse(p[1]);
                jump = int.Parse(p[2]);
            }

            if (jump != 10001)
                throw new NotSupportedException(
                    "Custom spring jump recovery requires the Extended Variants capability, which is not available in this port.");
            return new SpringRecovery(dash, sta);
        }

        internal SavedPlayerData SavePlayerData(RoomPlayerSession player) => new(player);

        internal bool CanUse(SavedPlayerData prev) =>
            !(dashes < 0 && prev.PrevDashes < -dashes || stamina < 0 && prev.PrevStamina < -stamina);

        internal void Recover(RoomPlayerSession player, SavedPlayerData prev)
        {
            if (dashes == 10001) player.Data.Dashes = prev.PrevDashes;
            else if (dashes < 0) player.Data.Dashes = prev.PrevDashes + dashes;
            else if (dashes < 10000) player.Data.Dashes = dashes;
            if (stamina == 10001) player.Data.Stamina = prev.PrevStamina;
            else if (stamina < 0) player.Data.Stamina = prev.PrevStamina + stamina;
            else if (stamina < 10000) player.Data.Stamina = stamina;
        }
    }
}
