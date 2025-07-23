using HarmonyLib;
using ProjectM;
using ProjectM.UI;
using Stunlock.Core;
using Unity.Entities;
using UnityEngine;
using Eclipse.Utilities.Extensions;

namespace Eclipse.Patches;

[HarmonyPatch]
internal static class ServantMenuMapperPatch
{
    const string SPELL_POWER = "Spell Power";
    const string HUNT_PROFICIENCY = "Hunt Proficiency";

    public static readonly Dictionary<PrefabGUID, string> ShinyBuffColorHexMap = new()
    {
        { new(348724578), "<color=#A020F0>Chaos</color>" },   // ignite purple
        { new(-1576512627), "<color=#FFD700>Storm</color>" },  // static yellow
        { new(-1246704569), "<color=#FF0000>Blood</color>" },  // leech red
        { new(1723455773), "<color=#008080>Illusion</color>" },   // weaken teal
        { new(27300215), "<color=#00FFFF>Frost</color>" },     // chill cyan
        { new(-325758519), "<color=#00FF00>Unholy</color>" }    // condemn green
    };

    static ServantInventorySubMenu _familiarServantMenu;
    static Entity _familiarServant;
    static Entity _familiar;

    [HarmonyPatch(typeof(ServantInventorySubMenuMapper), nameof(ServantInventorySubMenuMapper.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ServantInventorySubMenuMapper __instance)
    {
        if (!Core._initialized) return;
        else if (__instance._Target.HasValue())
        {
            Entity servant = __instance._Target;

            if (servant.Equals(_familiarServant))
            {
                // _familiarServantMenu?.Expertise.ForceSet(SPELL_POWER);
                // _familiarServantMenu?.HuntProficiency.ForceSet(shinyBuff);
            }
            else if (!servant.HasConnectedCoffin())
            {
                _familiarServant = servant;
                _familiarServantMenu = __instance._Menu;

                Entity playerCharacter = __instance.GetLocalPlayer();

                /*
                [Warning:   Eclipse] Follower - CHAR_ChurchOfLight_Paladin_VBlood PrefabGuid(-740796338)
                [Warning:   Eclipse] FollowerBuff - Buff_General_Wounded_Tracker PrefabGuid(224060472)
                [Warning:   Eclipse] FollowerBuff - Storm_Vampire_Buff_Static PrefabGuid(-1576512627)
                [Warning:   Eclipse] FollowerBuff - AB_Militia_HoundMaster_QuickShot_Buff PrefabGuid(1520432556)
                */

                if (playerCharacter.TryGetBuffer<FollowerBuffer>(out var buffer))
                {
                    foreach (FollowerBuffer follower in buffer)
                    {
                        Entity familiar = follower.Entity.GetEntityOnServer();

                        if (familiar.Exists())
                        {
                            _familiar = familiar;
                            var matchingBuff = ShinyBuffColorHexMap.FirstOrDefault(buff => _familiar.HasBuff(buff.Key));
                            string shinyBuff = matchingBuff.Value ?? "?";
                            // Core.Log.LogWarning($"Servant shiny buff - {shinyBuff}|{matchingBuff.Key.GetPrefabName()}|{(_familiar.TryGetBuff(matchingBuff.Key, out Entity buffEntity) ? buffEntity.Read<Buff>().Stacks : -1)}");
                        }
                    }
                }

                // ModifyFamiliarServantMenu(_familiarServantMenu.gameObject);
            }
            else if (servant.Exists())
            {
                _familiarServant = Entity.Null;
                _familiar = Entity.Null;
                // RestoreServantMenu(_familiarServantMenu.gameObject);
            }
        }
    }
    static void ModifyFamiliarServantMenu(GameObject menu)
    {

    }
    static void RestoreServantMenu(GameObject menu)
    {

    }
}
