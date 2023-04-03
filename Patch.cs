using FrooxEngine;
using HarmonyLib;
using NeosModLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;

namespace CanIJoinYou
{
    public class Patch : NeosMod
    {
        public override string Name => "CanIJoinYou";
        public override string Author => "LeCloutPanda";
        public override string Version => "1.0.0";
        public override string Link => "Deez Nuts";

        public static ModConfiguration config;

        public enum Responses
        {
            Request_Denied = 0,
            Unauthorized_Access = 1,
            Access_Forbidden = 2,
            Connection_Refused = 3,
        }

        public static string[] errors = { 
            "Request denied.",
            "Unauthorized Access",
            "Access Forbidden",
            "Connection Refused" };


        [AutoRegisterConfigKey] private static ModConfigurationKey<bool> ENABLED = new ModConfigurationKey<bool>("Mod Active", "", () => false);
        [AutoRegisterConfigKey] private static ModConfigurationKey<Responses> RESPONSE = new ModConfigurationKey<Responses>("Response", "Generic response sent to the user upon denying their request.", () => Responses.Request_Denied);

        class CanIJoinYou
        {
            [HarmonyPatch(typeof(World), "VerifyJoinRequest")]
            static async Task<JoinGrant> Postfix(Task<JoinGrant> __result, World __instance, JoinRequestData joinRequest)
            {
                // Just let the user in if the mod is off
                if (!config.GetValue(ENABLED))
                    return await __result;

                // Get the response
                var rejectMessage = errors[((int)config.GetValue(RESPONSE))];                    

                // Prompt the host with the choices
                switch (await __instance.Engine.Security.RequestAccessPermission($"{joinRequest.userID}", 69420, "Wants to join."))
                {
                    case HostAccessPermission.Allowed:
                        __instance.Engine.Security.RemoveAccessPermission($"{joinRequest.userID}", 69420);
                        return JoinGrant.Allow();
                        break;
                    case HostAccessPermission.Denied:
                        __instance.Engine.Security.RemoveAccessPermission($"{joinRequest.userID}", 69420);
                        return JoinGrant.Deny(rejectMessage);
                        break;
                    case HostAccessPermission.Ignored:
                        __instance.Engine.Security.RemoveAccessPermission($"{joinRequest.userID}", 69420);
                        return JoinGrant.Deny(rejectMessage);
                        break;
                }

                // Because this uses the Network Access stuff we gotta remove the new entry otherwise it will just keep denying/allowing the user who would like to join based on if the host says yes or no
                // Note: this can be use for a later date for like a white/black list type of things
                __instance.Engine.Security.RemoveAccessPermission($"{joinRequest.userID}", 69420);

                // Finally just wrap all this up
                return await __result;
            }
        }
    }
}
