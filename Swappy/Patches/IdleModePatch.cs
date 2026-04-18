using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using NorthwoodLib.Pools;
using Swappy.Managers;

namespace Swappy.Patches;

[HarmonyPatch(typeof(IdleMode))]
internal static class IdleModePatch
{
    [HarmonyPatch(nameof(IdleMode.SetIdleMode), [typeof(bool), typeof(bool)])]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Shared.Rent(instructions);
        
        int index = newInstructions.FindIndex(x => x.opcode == OpCodes.Call && x.operand is MethodInfo m && 
                                                   m == AccessTools.PropertySetter(typeof(UnityEngine.Time), nameof(UnityEngine.Time.timeScale)) &&
                                                   newInstructions[newInstructions.IndexOf(x) - 1].opcode == OpCodes.Ldc_R4 &&
                                                   newInstructions[newInstructions.IndexOf(x) - 1].operand is 1f);
        
        newInstructions.InsertRange(index + 1, 
[
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(IdleModePatch), nameof(OnExitIdleMode)))
        ]);
        
        foreach (CodeInstruction instruction in newInstructions)
            yield return instruction;
        
        ListPool<CodeInstruction>.Shared.Return(newInstructions);
    }

    private static async void OnExitIdleMode()
    {
        if (!Swappy.Singleton.Config.CheckOnExitIdle)
            return;
        
        Logger.Info("Exiting idle mode. Checking for updates...");
        
        int updatedCount = await UpdateManager.CheckForUpdates();
        
        if (updatedCount > 0)
            Round.Restart();
    }
}