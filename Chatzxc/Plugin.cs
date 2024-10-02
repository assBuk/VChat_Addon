using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using ValheimMod.Patches;

namespace ValheimMod;

[BepInPlugin($"{modName}", $"{modGUID}", $"{modVersion}")]
public class Plugin : BaseUnityPlugin
{
	private const string modGUID = "Chatzxc";

	private const string modName = "Chatzxc";

	private const string modVersion = "1.1";

	private readonly Harmony harmony = new Harmony($"{modGUID}");

	private static Plugin Instance;

	internal ManualLogSource m;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		m = BepInEx.Logging.Logger.CreateLogSource($"{modGUID}");
		m.LogInfo($"{modGUID} mod active");
		harmony.PatchAll();
	}

	private void CreateModMenuObject()
	{
		GameObject obj = new GameObject("ModMenu");
		Object.DontDestroyOnLoad(obj);
		obj.hideFlags = HideFlags.HideAndDontSave;
	}
}
