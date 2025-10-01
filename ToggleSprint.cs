using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace ToggleSprint;

[BepInPlugin(PluginInfo.PLUGIN_GUID, "Toggle Sprint", PluginInfo.PLUGIN_VERSION)]
class Mod : BaseUnityPlugin
{
    void Awake()
    {
        this.enabled = base.Config.Bind("General", "Enabled", true, "Enable toggle sprint");
        Mod.Instance = this;
        new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();
    }

    static Mod? Instance = null;
    new ConfigEntry<bool>? enabled = null;

    bool sprinting = false;
    bool dashPressedPrev = false;

    void UpdateSprinting()
    {
        if (!this.enabled!.Value || !TryUpdateSprinting())
        {
            this.sprinting = false;
            this.dashPressedPrev = false;
        }

        bool TryUpdateSprinting()
        {
            var hc = HeroController.SilentInstance;
            if (hc == null) return false;

            var ih = hc.inputHandler;
            if (ih == null) return false;

            var a = ih.inputActions as HeroActions;
            if (a == null) return false;

            var dashIsPressed = a.Dash.IsPressed;
            var moveIsPressed = a.Up.IsPressed || a.Down.IsPressed ||
                a.Right.IsPressed || a.Left.IsPressed;

            if (this.sprinting && dashIsPressed && !this.dashPressedPrev) this.sprinting = false;
            else if (dashIsPressed) this.sprinting = true;
            else if (!moveIsPressed) this.sprinting = false;

            a.Dash.thisState.Set(this.sprinting);
            this.dashPressedPrev = dashIsPressed;

            return true;
        }
    }

    [HarmonyPatch(typeof(InControl.InputManager), nameof(InControl.InputManager.UpdateInternal))]
    static class PeriodicallyUpdateSprinting
    {
        static void Postfix() => Mod.Instance!.UpdateSprinting();
    }
}
