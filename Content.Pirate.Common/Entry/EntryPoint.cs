// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Pirate.Common.IoC;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;

namespace Content.Pirate.Common.Entry;

// EntryPoint is marked as GameShared for module registration purposes.
public sealed class EntryPoint : GameShared
{
    public override void PreInit()
    {
        IoCManager.InjectDependencies(this);
        CommonPirateContentIoC.Register();
    }
}
