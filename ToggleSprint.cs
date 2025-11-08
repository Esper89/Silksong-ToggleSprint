using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace ToggleSprint;

[BepInAutoPlugin]
sealed partial class Mod : BaseUnityPlugin
{
    void Awake()
    {
        Mod._instance = this;
        this._config = new(base.Config);
        new Harmony(Mod.Id).PatchAll();
    }

    static Mod? _instance = null;
    static Mod Instance => Mod._instance ? Mod._instance :
        throw new NullReferenceException($"{nameof(Mod)} accessed before {nameof(Awake)}");

    ConfigEntries? _config = null;
    new ConfigEntries Config => this._config!;

    sealed class ConfigEntries(ConfigFile file)
    {
        internal bool Enabled => this._enabled.Value;
        ConfigEntry<bool> _enabled = file.Bind("General", "Enabled", true, "Enable toggle sprint");
    }

    bool sprinting = false;
    bool dashPressedPrev = false;

    void UpdateSprinting()
    {
        if (!this.Config.Enabled || !TryUpdateSprinting())
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
        static void Postfix() => Mod.Instance.UpdateSprinting();
    }
}
