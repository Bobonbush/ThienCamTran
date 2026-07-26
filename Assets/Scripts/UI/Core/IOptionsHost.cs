using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

namespace Game.UI
{
    /// <summary>
    /// The assets <see cref="MainMenuOptionsPanel"/> needs in order to drive the authored
    /// "Option_Panel " prefab. Exists so that panel is not tied to <see cref="MainMenuController"/>
    /// and can be reused by the in-game book overlay (<see cref="SettingsMenuPanel"/>), which lives
    /// in a persistent prefab rather than the menu scene.
    /// </summary>
    public interface IOptionsHost
    {
        AudioMixer AudioMixer { get; }
        InputActionAsset InputActions { get; }
        Sprite VideoLabelSprite { get; }
        Sprite AudioLabelSprite { get; }
        Sprite ControlLabelSprite { get; }

        /// <summary>Sprite swapped in when a menu button is hovered/selected.</summary>
        Sprite SelectedButtonSprite { get; }
    }
}
