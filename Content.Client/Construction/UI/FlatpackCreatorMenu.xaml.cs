// SPDX-FileCopyrightText: 2024 12rabbits <53499656+12rabbits@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Alzore <140123969+Blackern5000@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 ArtisticRoomba <145879011+ArtisticRoomba@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Brandon Hu <103440971+Brandon-Huu@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Dimastra <65184747+Dimastra@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Dimastra <dimastra@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Ed <96445749+TheShuEd@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Emisse <99158783+Emisse@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Eoin Mcloughlin <helloworld@eoinrul.es>
// SPDX-FileCopyrightText: 2024 IProduceWidgets <107586145+IProduceWidgets@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 JIPDawg <51352440+JIPDawg@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 JIPDawg <JIPDawg93@gmail.com>
// SPDX-FileCopyrightText: 2024 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Mervill <mervills.email@gmail.com>
// SPDX-FileCopyrightText: 2024 Moomoobeef <62638182+Moomoobeef@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 PJBot <pieterjan.briers+bot@gmail.com>
// SPDX-FileCopyrightText: 2024 Pieter-Jan Briers <pieterjan.briers+git@gmail.com>
// SPDX-FileCopyrightText: 2024 Pieter-Jan Briers <pieterjan.briers@gmail.com>
// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2024 PopGamer46 <yt1popgamer@gmail.com>
// SPDX-FileCopyrightText: 2024 PursuitInAshes <pursuitinashes@gmail.com>
// SPDX-FileCopyrightText: 2024 QueerNB <176353696+QueerNB@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Saphire Lattice <lattice@saphi.re>
// SPDX-FileCopyrightText: 2024 ShadowCommander <10494922+ShadowCommander@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Simon <63975668+Simyon264@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 SpeltIncorrectyl <66873282+SpeltIncorrectyl@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Spessmann <156740760+Spessmann@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Thomas <87614336+Aeshus@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Winkarst <74284083+Winkarst-cpu@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 deltanedas <39013340+deltanedas@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 deltanedas <@deltanedas:kde.org>
// SPDX-FileCopyrightText: 2024 eoineoineoin <github@eoinrul.es>
// SPDX-FileCopyrightText: 2024 github-actions[bot] <41898282+github-actions[bot]@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 lzk <124214523+lzk228@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 slarticodefast <161409025+slarticodefast@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 stellar-novas <stellar_novas@riseup.net>
// SPDX-FileCopyrightText: 2024 themias <89101928+themias@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Client.Materials;
using Content.Client.Materials.UI;
using Content.Client.Message;
using Content.Client.UserInterface.Controls;
using Content.Shared.Construction.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Materials;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Construction.UI;

[GenerateTypedNameReferences]
public sealed partial class FlatpackCreatorMenu : FancyWindow
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private readonly ItemSlotsSystem _itemSlots;
    private readonly FlatpackSystem _flatpack;
    private readonly MaterialStorageSystem _materialStorage;

    private EntityUid _owner;

    [ValidatePrototypeId<EntityPrototype>]
    public const string NoBoardEffectId = "FlatpackerNoBoardEffect";

    private EntityUid? _currentBoard = EntityUid.Invalid;

    public event Action? PackButtonPressed;

    public FlatpackCreatorMenu()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        _itemSlots = _entityManager.System<ItemSlotsSystem>();
        _flatpack = _entityManager.System<FlatpackSystem>();
        _materialStorage = _entityManager.System<MaterialStorageSystem>();

        PackButton.OnPressed += _ => PackButtonPressed?.Invoke();

        InsertLabel.SetMarkup(Loc.GetString("flatpacker-ui-insert-board"));
    }

    public void SetEntity(EntityUid uid)
    {
        _owner = uid;
        MaterialStorageControl.SetOwner(uid);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_entityManager.TryGetComponent<FlatpackCreatorComponent>(_owner, out var flatpacker) ||
            !_itemSlots.TryGetSlot(_owner, flatpacker.SlotId, out var itemSlot))
            return;

        if (flatpacker.Packing)
        {
            PackButton.Disabled = true;
        }
        else if (_currentBoard != null)
        {
            Dictionary<string, int> cost;
            if (_entityManager.TryGetComponent<MachineBoardComponent>(_currentBoard, out var machineBoardComp))
                cost = _flatpack.GetFlatpackCreationCost((_owner, flatpacker), (_currentBoard.Value, machineBoardComp));
            else
                cost = _flatpack.GetFlatpackCreationCost((_owner, flatpacker), null);

            PackButton.Disabled = !_materialStorage.CanChangeMaterialAmount(_owner, cost);
        }

        if (_currentBoard == itemSlot.Item)
            return;

        _currentBoard = itemSlot.Item;
        CostHeaderLabel.Visible = _currentBoard != null;
        InsertLabel.Visible = _currentBoard == null;

        if (_currentBoard is not null)
        {
            string? prototype = null;
            Dictionary<string, int>? cost = null;

            if (_entityManager.TryGetComponent<MachineBoardComponent>(_currentBoard, out var newMachineBoardComp))
            {
                prototype = newMachineBoardComp.Prototype;
                cost = _flatpack.GetFlatpackCreationCost((_owner, flatpacker), (_currentBoard.Value, newMachineBoardComp));
            }
            else if (_entityManager.TryGetComponent<ComputerBoardComponent>(_currentBoard, out var computerBoard))
            {
                prototype = computerBoard.Prototype;
                cost = _flatpack.GetFlatpackCreationCost((_owner, flatpacker), null);
            }

            if (prototype is not null && cost is not null)
            {
                var proto = _prototypeManager.Index<EntityPrototype>(prototype);
                MachineSprite.SetPrototype(prototype);
                MachineNameLabel.SetMessage(proto.Name);
                CostLabel.SetMarkup(GetCostString(cost));
            }
        }
        else
        {
            MachineSprite.SetPrototype(NoBoardEffectId);
            CostLabel.SetMessage(Loc.GetString("flatpacker-ui-no-board-label"));
            MachineNameLabel.SetMessage(" ");
            PackButton.Disabled = true;
        }
    }

    private string GetCostString(Dictionary<string, int> costs)
    {
        var orderedCosts = costs.OrderBy(p => p.Value).ToArray();
        var msg = new FormattedMessage();
        for (var i = 0; i < orderedCosts.Length; i++)
        {
            var (mat, amount) = orderedCosts[i];

            var matProto = _prototypeManager.Index<MaterialPrototype>(mat);

            var sheetVolume = _materialStorage.GetSheetVolume(matProto);
            var sheets = (float) -amount / sheetVolume;
            var amountText = Loc.GetString("lathe-menu-material-amount",
                ("amount", sheets),
                ("unit", Loc.GetString(matProto.Unit)));
            var text = Loc.GetString("lathe-menu-tooltip-display",
                ("amount", amountText),
                ("material", Loc.GetString(matProto.Name)));

            msg.TryAddMarkup(text, out _);

            if (i != orderedCosts.Length - 1)
                msg.PushNewline();
        }

        return msg.ToMarkup();
    }
}
