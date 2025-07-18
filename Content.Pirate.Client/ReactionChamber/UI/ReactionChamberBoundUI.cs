using System;
using Content.Pirate.Shared.ReactionChamber.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Pirate.Client.ReactionChamber.UI;

[UsedImplicitly]
public sealed class ReactionChamberBoundUI : BoundUserInterface
{


    [ViewVariables]
    private ReactionChamberWindow _window;
    public ReactionChamberBoundUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _window = new ReactionChamberWindow();
        var activeButton = _window.FindControl<TextureButton>("ActiveButton");
        activeButton.OnButtonDown += _ => onActiveBtnPressed(!_window.Active);
        activeButton.TexturePath = _window.Active ? "/Textures/_Pirate/UserInterface/Buttons/switch.rsi/switch_on.png" : "/Textures/_Pirate/UserInterface/Buttons/switch.rsi/switch_off.png";
        var tempField = _window.FindControl<FloatSpinBox>("TempField");
        tempField.OnValueChanged += _ => onTempFieldEntered(_window.FindControl<FloatSpinBox>("TempField").Value);
    }
    protected override void Open()
    {
        base.Open();
        _window.OnClose += Close;

        if (State != null)
        {
            UpdateState(State);
        }

        _window.OpenCentered();
    }
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        var castState = (ReactionChamberBoundUIState) state;
        _window.UpdateState(castState);
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _window?.Close();
    }
    private void onActiveBtnPressed(bool active)
    {
        _window.UpdateActiveButtonUI(active);
        //Sends new data to system
        SendMessage(new ReactionChamberActiveChangeMessage(active));
    }
    private void onTempFieldEntered(float temp)
    {
        _window.SetTemp(temp);
        SendMessage(new ReactionChamberTempChangeMessage(temp));
    }

}
