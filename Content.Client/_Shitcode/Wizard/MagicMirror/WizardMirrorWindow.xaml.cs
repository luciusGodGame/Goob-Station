// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Client.Humanoid;
using Content.Client.Lobby;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Client.Utility;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Direction = Robust.Shared.Maths.Direction;

namespace Content.Client._Shitcode.Wizard.MagicMirror;

[GenerateTypedNameReferences]
public sealed partial class WizardMirrorWindow : DefaultWindow
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly MarkingManager _markingManager = default!;
    private readonly LobbyUIController _controller;

    public HashSet<ProtoId<SpeciesPrototype>>? AllowedSpecies;

    /// <summary>
    /// If we're attempting to save.
    /// </summary>
    public event Action? Save;

    /// <summary>
    /// Entity used for the profile editor preview
    /// </summary>
    public EntityUid PreviewDummy;

    /// <summary>
    /// The work in progress profile being edited.
    /// </summary>
    public HumanoidCharacterProfile? Profile;

    public HumanoidCharacterProfile? LoadedProfile;

    private readonly List<SpeciesPrototype> _species = new();

    private Direction _previewRotation = Direction.North;

    private readonly ColorSelectorSliders _rgbSkinColorSelector;

    private bool _isDirty;

    public WizardMirrorWindow()
    {
        IoCManager.InjectDependencies(this);
        RobustXamlLoader.Load(this);
        _controller = UserInterfaceManager.GetUIController<LobbyUIController>();

        SaveButton.OnPressed += _ =>
        {
            Save?.Invoke();
            Close();
        };

        #region Left

        #region Name

        NameEdit.OnTextChanged += args => { SetName(args.Text); };
        NameRandomize.OnPressed += _ => RandomizeName();
        RandomizeEverythingButton.OnPressed += _ => { RandomizeEverything(); };

        #endregion Name

        #region Appearance

        TabContainer.SetTabTitle(0, Loc.GetString("humanoid-profile-editor-appearance-tab"));

        #region Sex

        SexButton.OnItemSelected += args =>
        {
            SexButton.SelectId(args.Id);
            SetSex((Sex) args.Id);
        };

        #endregion Sex

        #region Gender

        PronounsButton.AddItem(Loc.GetString("humanoid-profile-editor-pronouns-male-text"), (int) Gender.Male);
        PronounsButton.AddItem(Loc.GetString("humanoid-profile-editor-pronouns-female-text"), (int) Gender.Female);
        PronounsButton.AddItem(Loc.GetString("humanoid-profile-editor-pronouns-epicene-text"), (int) Gender.Epicene);
        PronounsButton.AddItem(Loc.GetString("humanoid-profile-editor-pronouns-neuter-text"), (int) Gender.Neuter);

        PronounsButton.OnItemSelected += args =>
        {
            PronounsButton.SelectId(args.Id);
            SetGender((Gender) args.Id);
        };

        #endregion Gender

        RefreshSpecies();

        SpeciesButton.OnItemSelected += args =>
        {
            SpeciesButton.SelectId(args.Id);
            SetSpecies(_species[args.Id].ID);
            UpdateHairPickers();
            OnSkinColorOnValueChanged();
        };

        #region Skin

        Skin.OnValueChanged += _ =>
        {
            OnSkinColorOnValueChanged();
        };

        RgbSkinColorContainer.AddChild(_rgbSkinColorSelector = new ColorSelectorSliders());
        _rgbSkinColorSelector.OnColorChanged += _ =>
        {
            OnSkinColorOnValueChanged();
        };

        #endregion

        #region Hair

        HairStylePicker.OnMarkingSelect += newStyle =>
        {
            if (Profile is null)
                return;
            ClearMarking(Profile.Appearance.HairStyleId);
            Profile = Profile.WithCharacterAppearance(
                Profile.Appearance.WithHairStyleName(newStyle.id));
            ReloadPreview();
        };

        HairStylePicker.OnColorChanged += newColor =>
        {
            if (Profile is null)
                return;
            ClearMarking(Profile.Appearance.HairStyleId);
            Profile = Profile.WithCharacterAppearance(
                Profile.Appearance.WithHairColor(newColor.marking.MarkingColors[0]));
            UpdateCMarkingsHair();
            ReloadPreview();
        };

        FacialHairPicker.OnMarkingSelect += newStyle =>
        {
            if (Profile is null)
                return;

            ClearMarking(Profile.Appearance.FacialHairStyleId);
            Profile = Profile.WithCharacterAppearance(
                Profile.Appearance.WithFacialHairStyleName(newStyle.id));
            ReloadPreview();
        };

        FacialHairPicker.OnColorChanged += newColor =>
        {
            if (Profile is null)
                return;
            ClearMarking(Profile.Appearance.FacialHairStyleId);
            Profile = Profile.WithCharacterAppearance(
                Profile.Appearance.WithFacialHairColor(newColor.marking.MarkingColors[0]));
            UpdateCMarkingsFacialHair();
            ReloadPreview();
        };

        HairStylePicker.OnSlotRemove += _ =>
        {
            if (Profile is null)
                return;
            ClearMarking(Profile.Appearance.HairStyleId);
            Profile = Profile.WithCharacterAppearance(
                Profile.Appearance.WithHairStyleName(HairStyles.DefaultHairStyle)
            );
            UpdateHairPickers();
            UpdateCMarkingsHair();
            ReloadPreview();
        };

        FacialHairPicker.OnSlotRemove += _ =>
        {
            if (Profile is null)
                return;
            ClearMarking(Profile.Appearance.FacialHairStyleId);
            Profile = Profile.WithCharacterAppearance(
                Profile.Appearance.WithFacialHairStyleName(HairStyles.DefaultFacialHairStyle)
            );
            UpdateHairPickers();
            UpdateCMarkingsFacialHair();
            ReloadPreview();
        };

        HairStylePicker.OnSlotAdd += delegate
        {
            if (Profile is null)
                return;

            var ckey = Robust.Shared.IoC.IoCManager.Resolve<Robust.Client.Player.IPlayerManager>().LocalSession?.Name; // Pirate ckey for restricted players
            var hair = _markingManager.MarkingsByCategoryAndSpecies(MarkingCategories.Hair, Profile.Species, ckey) // Pirate ckey for restricted players
                .Keys
                .FirstOrDefault();

            if (string.IsNullOrEmpty(hair))
                return;

            ClearMarking(Profile.Appearance.HairStyleId);

            Profile = Profile.WithCharacterAppearance(
                Profile.Appearance.WithHairStyleName(hair)
            );

            UpdateHairPickers();
            UpdateCMarkingsHair();
            ReloadPreview();
        };

        FacialHairPicker.OnSlotAdd += delegate
        {
            if (Profile is null)
                return;

            var ckey = Robust.Shared.IoC.IoCManager.Resolve<Robust.Client.Player.IPlayerManager>().LocalSession?.Name; // Pirate ckey for restricted players
            var hair = _markingManager.MarkingsByCategoryAndSpecies(MarkingCategories.FacialHair, Profile.Species, ckey) // Pirate ckey for restricted players
                .Keys
                .FirstOrDefault();

            if (string.IsNullOrEmpty(hair))
                return;

            ClearMarking(Profile.Appearance.FacialHairStyleId);

            Profile = Profile.WithCharacterAppearance(
                Profile.Appearance.WithFacialHairStyleName(hair)
            );

            UpdateHairPickers();
            UpdateCMarkingsFacialHair();
            ReloadPreview();
        };

        #endregion Hair

        #region Eyes

        EyeColorPicker.OnEyeColorPicked += newColor =>
        {
            if (Profile is null)
                return;
            Profile = Profile.WithCharacterAppearance(
                Profile.Appearance.WithEyeColor(newColor));
            Markings.CurrentEyeColor = Profile.Appearance.EyeColor;
            ReloadProfilePreview();
        };

        #endregion Eyes

        #endregion Appearance

        #region Markings

        TabContainer.SetTabTitle(1, Loc.GetString("humanoid-profile-editor-markings-tab"));

        Markings.OnMarkingAdded += OnMarkingChange;
        Markings.OnMarkingRemoved += OnMarkingChange;
        Markings.OnMarkingColorChange += OnMarkingChange;
        Markings.OnMarkingRankChange += OnMarkingChange;

        #endregion Markings

        #region Dummy

        SpriteRotateLeft.OnPressed += _ =>
        {
            _previewRotation = _previewRotation.TurnCw();
            SetPreviewRotation(_previewRotation);
        };
        SpriteRotateRight.OnPressed += _ =>
        {
            _previewRotation = _previewRotation.TurnCcw();
            SetPreviewRotation(_previewRotation);
        };

        #endregion Dummy

        #endregion Left

        IsDirty = false;
    }

    private void ClearMarking(string id)
    {
        Profile?.Appearance.Markings.RemoveAll(x => x.MarkingId == id);
    }

    /// <summary>
    /// Refreshes the species selector.
    /// </summary>
    public void RefreshSpecies()
    {
        SpeciesButton.Clear();
        _species.Clear();

        if (AllowedSpecies != null)
        {
            _species.AddRange(_prototypeManager.EnumeratePrototypes<SpeciesPrototype>()
                .Where(o => AllowedSpecies.Contains(o.ID)));
            if (Profile != null && _prototypeManager.TryIndex(Profile.Species, out var prototype) && !_species.Contains(prototype))
                _species.Add(prototype);
        }
        else
            _species.AddRange(_prototypeManager.EnumeratePrototypes<SpeciesPrototype>().Where(o => o.RoundStart));
        var speciesIds = _species.Select(o => o.ID).ToList();

        for (var i = 0; i < _species.Count; i++)
        {
            var name = Loc.GetString(_species[i].Name);
            SpeciesButton.AddItem(name, i);

            if (Profile?.Species.Equals(_species[i].ID) == true)
            {
                SpeciesButton.SelectId(i);
            }
        }

        // If our species isn't available then reset it to default.
        if (Profile != null)
        {
            if (!speciesIds.Contains(Profile.Species))
            {
                SetSpecies(SharedHumanoidAppearanceSystem.DefaultSpecies);
            }
        }
    }

    private void SetDirty()
    {
        // If it equals default then reset the button.
        if (Profile == null || LoadedProfile == null || LoadedProfile.MemberwiseEquals(Profile))
        {
            IsDirty = false;
            return;
        }

        IsDirty = true;
    }

    /// <summary>
    /// Reloads the entire dummy entity for preview.
    /// </summary>
    /// <remarks>
    /// This is expensive so not recommended to run if you have a slider.
    /// </remarks>
    private void ReloadPreview()
    {
        _entManager.DeleteEntity(PreviewDummy);
        PreviewDummy = EntityUid.Invalid;

        if (Profile == null || !_prototypeManager.HasIndex(Profile.Species))
            return;

        PreviewDummy = _controller.LoadProfileEntity(Profile, null, false);
        SpriteView.SetEntity(PreviewDummy);
        _entManager.System<MetaDataSystem>().SetEntityName(PreviewDummy, Profile.Name);

        // Check and set the dirty flag to enable the save/reset buttons as appropriate.
        SetDirty();
    }

    /// <summary>
    /// Sets the editor to the specified profile with the specified slot.
    /// </summary>
    public void SetProfile(HumanoidCharacterProfile? profile)
    {
        Profile = profile?.Clone();
        IsDirty = false;

        UpdateNameEdit();
        UpdateSexControls();
        UpdateGenderControls();
        UpdateSkinColor();
        UpdateEyePickers();
        UpdateSaveButton();
        UpdateMarkings();
        UpdateHairPickers();
        UpdateCMarkingsHair();
        UpdateCMarkingsFacialHair();

        RefreshSpecies();
        ReloadPreview();
    }


    /// <summary>
    /// A slim reload that only updates the entity itself and not any of the job entities, etc.
    /// </summary>
    private void ReloadProfilePreview()
    {
        if (Profile == null || !_entManager.EntityExists(PreviewDummy))
            return;

        _entManager.System<HumanoidAppearanceSystem>().LoadProfile(PreviewDummy, Profile);

        // Check and set the dirty flag to enable the save/reset buttons as appropriate.
        SetDirty();
    }

    private void OnMarkingChange(MarkingSet markings)
    {
        if (Profile is null)
            return;

        Profile = Profile.WithCharacterAppearance(Profile.Appearance.WithMarkings(markings.GetForwardEnumerator()
            .Where(x => x.MarkingId != Profile.Appearance.HairStyleId &&
                        x.MarkingId != Profile.Appearance.FacialHairStyleId)
            .ToList()));
        ReloadProfilePreview();
    }

    // Allow all the possible skin colors
    private void OnSkinColorOnValueChanged()
    {
        if (Profile is null)
            return;

        if (!RgbSkinColorContainer.Visible)
        {
            Skin.Visible = false;
            RgbSkinColorContainer.Visible = true;
        }

        Markings.CurrentSkinColor = _rgbSkinColorSelector.Color;
        Profile = Profile.WithCharacterAppearance(Profile.Appearance.WithSkinColor(_rgbSkinColorSelector.Color));

        ReloadProfilePreview();
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();
        ReloadPreview();
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();
        _entManager.DeleteEntity(PreviewDummy);
        PreviewDummy = EntityUid.Invalid;
    }

    private void SetSex(Sex newSex)
    {
        Profile = Profile?.WithSex(newSex);
        // for convenience, default to most common gender when new sex is selected
        switch (newSex)
        {
            case Sex.Male:
                Profile = Profile?.WithGender(Gender.Male);
                break;
            case Sex.Female:
                Profile = Profile?.WithGender(Gender.Female);
                break;
            default:
                Profile = Profile?.WithGender(Gender.Epicene);
                break;
        }

        UpdateGenderControls();
        Markings.SetSex(newSex);
        ReloadPreview();
    }

    private void SetGender(Gender newGender)
    {
        Profile = Profile?.WithGender(newGender);
        ReloadPreview();
    }

    private void SetSpecies(string newSpecies)
    {
        Profile = Profile?.WithSpecies(newSpecies);
        OnSkinColorOnValueChanged(); // Species may have special color prefs, make sure to update it.
        Markings.SetSpecies(newSpecies); // Repopulate the markings tab as well.
        UpdateSexControls(); // update sex for new species
        ReloadPreview();
    }

    private void SetName(string newName)
    {
        Profile = Profile?.WithName(newName);
        SetDirty();

        if (!IsDirty)
            return;

        _entManager.System<MetaDataSystem>().SetEntityName(PreviewDummy, newName);
    }

    public bool IsDirty
    {
        get => _isDirty;
        set
        {
            if (_isDirty == value)
                return;

            _isDirty = value;
            UpdateSaveButton();
        }
    }

    private void UpdateNameEdit()
    {
        NameEdit.Text = Profile?.Name ?? "";
    }

    private void UpdateSexControls()
    {
        if (Profile == null)
            return;

        SexButton.Clear();

        var sexes = new List<Sex>();

        if (_prototypeManager.TryIndex(Profile.Species, out var speciesProto))
        {
            foreach (var sex in speciesProto.Sexes)
            {
                sexes.Add(sex);
            }
        }
        else
        {
            sexes.Add(Sex.Unsexed);
        }

        // add button for each sex
        foreach (var sex in sexes)
        {
            SexButton.AddItem(Loc.GetString($"humanoid-profile-editor-sex-{sex.ToString().ToLower()}-text"), (int) sex);
        }

        if (sexes.Contains(Profile.Sex))
            SexButton.SelectId((int) Profile.Sex);
        else
            SexButton.SelectId((int) sexes[0]);
    }

    private void UpdateSkinColor()
    {
        if (Profile == null)
            return;

        if (!RgbSkinColorContainer.Visible)
        {
            Skin.Visible = false;
            RgbSkinColorContainer.Visible = true;
        }

        // set the RGB values to the direct values otherwise
        _rgbSkinColorSelector.Color = Profile.Appearance.SkinColor;
    }

    private void UpdateMarkings()
    {
        if (Profile == null)
        {
            return;
        }

        Markings.SetData(Profile.Appearance.Markings,
            Profile.Species,
            Profile.Sex,
            Profile.Appearance.SkinColor,
            Profile.Appearance.EyeColor
        );
    }

    private void UpdateGenderControls()
    {
        if (Profile == null)
        {
            return;
        }

        PronounsButton.SelectId((int) Profile.Gender);
    }

    private void UpdateHairPickers()
    {
        if (Profile == null)
        {
            return;
        }

        var hairMarking = Profile.Appearance.HairStyleId switch
        {
            HairStyles.DefaultHairStyle => new List<Marking>(),
            _ => new() { new(Profile.Appearance.HairStyleId, new List<Color>() { Profile.Appearance.HairColor }) },
        };

        var facialHairMarking = Profile.Appearance.FacialHairStyleId switch
        {
            HairStyles.DefaultFacialHairStyle => new List<Marking>(),
            _ => new()
            {
                new(Profile.Appearance.FacialHairStyleId, new List<Color>() { Profile.Appearance.FacialHairColor })
            },
        };

        HairStylePicker.UpdateData(
            hairMarking,
            Profile.Species,
            1);
        FacialHairPicker.UpdateData(
            facialHairMarking,
            Profile.Species,
            1);
    }

    private void UpdateCMarkingsHair()
    {
        if (Profile == null)
        {
            return;
        }

        // hair color
        Color? hairColor = null;
        if (Profile.Appearance.HairStyleId != HairStyles.DefaultHairStyle &&
            _markingManager.Markings.TryGetValue(Profile.Appearance.HairStyleId, out var hairProto)
           )
        {
            if (_markingManager.CanBeApplied(Profile.Species, Profile.Sex, hairProto, _prototypeManager))
            {
                hairColor = _markingManager.MustMatchSkin(Profile.Species,
                    HumanoidVisualLayers.Hair,
                    out var _,
                    _prototypeManager)
                    ? Profile.Appearance.SkinColor
                    : Profile.Appearance.HairColor;
            }
        }

        if (hairColor != null)
        {
            Markings.HairMarking = new(Profile.Appearance.HairStyleId, new List<Color>() { hairColor.Value });
        }
        else
        {
            Markings.HairMarking = null;
        }
    }

    private void UpdateCMarkingsFacialHair()
    {
        if (Profile == null)
        {
            return;
        }

        // facial hair color
        Color? facialHairColor = null;
        if (Profile.Appearance.FacialHairStyleId != HairStyles.DefaultFacialHairStyle &&
            _markingManager.Markings.TryGetValue(Profile.Appearance.FacialHairStyleId, out var facialHairProto))
        {
            if (_markingManager.CanBeApplied(Profile.Species, Profile.Sex, facialHairProto, _prototypeManager))
            {
                facialHairColor = _markingManager.MustMatchSkin(Profile.Species,
                    HumanoidVisualLayers.Hair,
                    out var _,
                    _prototypeManager)
                    ? Profile.Appearance.SkinColor
                    : Profile.Appearance.FacialHairColor;
            }
        }

        if (facialHairColor != null)
        {
            Markings.FacialHairMarking = new(Profile.Appearance.FacialHairStyleId,
                new List<Color>() { facialHairColor.Value });
        }
        else
        {
            Markings.FacialHairMarking = null;
        }
    }

    private void UpdateEyePickers()
    {
        if (Profile == null)
        {
            return;
        }

        Markings.CurrentEyeColor = Profile.Appearance.EyeColor;
        EyeColorPicker.SetData(Profile.Appearance.EyeColor);
    }

    private void UpdateSaveButton()
    {
        SaveButton.Disabled = Profile is null || !IsDirty;
    }

    private void SetPreviewRotation(Direction direction)
    {
        SpriteView.OverrideDirection = (Direction) ((int) direction % 4 * 2);
    }

    private void RandomizeEverything()
    {
        Profile = HumanoidCharacterProfile.Random();
        if (LoadedProfile != null)
            Profile = Profile.WithAge(LoadedProfile.Age);
        SetDirty();
        SetProfile(Profile);
    }

    private void RandomizeName()
    {
        if (Profile == null)
            return;
        var name = HumanoidCharacterProfile.GetName(Profile.Species, Profile.Gender);
        SetName(name);
        UpdateNameEdit();
    }
}
