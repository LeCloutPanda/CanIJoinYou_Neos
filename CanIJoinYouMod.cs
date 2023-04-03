using FrooxEngine;
using HarmonyLib;
using NeosModLoader;
using System.Threading.Tasks;

namespace CanIJoinYou;

public class CanIJoinYouMod : NeosMod
{
    public override string Name => "CanIJoinYou";
    public override string Author => "LeCloutPanda";
    public override string Version => "1.0.1";
    public override string Link => "https://github.com/LeCloutPanda/CanIJoinYou";

    private static ModConfiguration _config;

    private enum Responses
    {
        RequestDenied = 0,
        UnauthorizedAccess = 1,
        AccessForbidden = 2,
        ConnectionRefused = 3,
    }

    private static readonly string[] Errors = { 
        "Request denied.",
        "Unauthorized Access",
        "Access Forbidden",
        "Connection Refused" };
    [AutoRegisterConfigKey]
    private static ModConfigurationKey<bool>
        _enabled = new ModConfigurationKey<bool>("Mod Active", "", () => false);
    [AutoRegisterConfigKey] private static ModConfigurationKey<Responses> _response =
        new ModConfigurationKey<Responses>("Response",
            "Generic response sent to the user upon denying their request.", () => Responses.RequestDenied);

    public override void OnEngineInit()
    {
        _config = GetConfiguration();
        _config?.Save(true);

        var harmony = new Harmony($"dev.{Author}.{Name}");
        harmony.PatchAll();
    }

    class CanIJoinYou
    {
        [HarmonyPatch(typeof(World), "VerifyJoinRequest")]
        static async Task<JoinGrant> Postfix(Task<JoinGrant> __result, World __instance, JoinRequestData joinRequest)
        {
            // Just let the user in if the mod is off
            if (!_config.GetValue(_enabled))
                return await __result;
            // Get the response
            var rejectMessage = Errors[(int)_config.GetValue(_response)];
            // Prompt the host with the choices
            switch (await __instance.Engine.Security.RequestAccessPermission($"{joinRequest.userID}", 69420,
                        "Wants to join."))
            {
                case HostAccessPermission.Allowed:
                    __instance.Engine.Security.RemoveAccessPermission($"{joinRequest.userID}", 69420);
                    return JoinGrant.Allow();
                case HostAccessPermission.Denied:
                case HostAccessPermission.Ignored:
                    __instance.Engine.Security.RemoveAccessPermission($"{joinRequest.userID}", 69420);
                    return JoinGrant.Deny(rejectMessage);
            }

            // Because this uses the Network Access stuff we gotta remove the new entry otherwise it will just keep denying/allowing the user who would like to join based on if the host says yes or no
            // Note: this can be use for a later date for like a white/black list type of things
            __instance.Engine.Security.RemoveAccessPermission($"{joinRequest.userID}", 69420);
            // Finally just wrap all this up
            return await __result;
        }
    }
}