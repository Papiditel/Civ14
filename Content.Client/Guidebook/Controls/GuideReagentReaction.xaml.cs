using System.Linq;
using Content.Client.Message;
using Content.Client.UserInterface.ControlExtensions;
using Content.Shared.Atmos.Prototypes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Localizations;
using JetBrains.Annotations;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Guidebook.Controls;

[UsedImplicitly, GenerateTypedNameReferences]
public sealed partial class GuideReagentReaction : BoxContainer, ISearchableControl
{
    [ValidatePrototypeId<MixingCategoryPrototype>]
    private const string DefaultMixingCategory = "DummyMix";

    private readonly IPrototypeManager _protoMan;

    public GuideReagentReaction(IPrototypeManager protoMan)
    {
        RobustXamlLoader.Load(this);
        _protoMan = protoMan;
    }

    public GuideReagentReaction(ReactionPrototype prototype, IPrototypeManager protoMan, IEntitySystemManager sysMan) : this(protoMan)
    {
        var reactantsLabel = ReactantsLabel;
        SetReagents(prototype.Reactants, ref reactantsLabel, protoMan);
        var productLabel = ProductsLabel;
        var products = new Dictionary<string, FixedPoint2>(prototype.Products);
        foreach (var (reagent, reactantProto) in prototype.Reactants)
        {
            if (reactantProto.Catalyst)
                products.Add(reagent, reactantProto.Amount);
        }
        SetReagents(products, ref productLabel, protoMan);

        var mixingCategories = new List<MixingCategoryPrototype>();
        if (prototype.MixingCategories != null)
        {
            foreach (var category in prototype.MixingCategories)
            {
                mixingCategories.Add(protoMan.Index(category));
            }
        }
        else
        {
            mixingCategories.Add(protoMan.Index<MixingCategoryPrototype>(DefaultMixingCategory));
        }
        SetMixingCategory(mixingCategories, prototype, sysMan);
    }

    public GuideReagentReaction(EntityPrototype prototype,
        Solution solution,
        IReadOnlyList<ProtoId<MixingCategoryPrototype>> categories,
        IPrototypeManager protoMan,
        IEntitySystemManager sysMan) : this(protoMan)
    {
        var icon = sysMan.GetEntitySystem<SpriteSystem>().GetPrototypeIcon(prototype).GetFrame(RsiDirection.South, 0);
        var entContainer = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            HorizontalAlignment = HAlignment.Center,
            Children =
            {
                new TextureRect
                {
                    Texture = icon
                }
            }
        };
        var nameLabel = new RichTextLabel();
        nameLabel.SetMarkup(Loc.GetString("guidebook-reagent-sources-ent-wrapper", ("name", prototype.Name)));
        entContainer.AddChild(nameLabel);
        ReactantsContainer.AddChild(entContainer);

        var productLabel = ProductsLabel;
        SetReagents(solution.Contents, ref productLabel, protoMan);
        SetMixingCategory(categories, null, sysMan);
    }

    public GuideReagentReaction(GasPrototype prototype,
        IReadOnlyList<ProtoId<MixingCategoryPrototype>> categories,
        IPrototypeManager protoMan,
        IEntitySystemManager sysMan) : this(protoMan)
    {
        ReactantsLabel.Visible = true;
        ReactantsLabel.SetMarkup(Loc.GetString("guidebook-reagent-sources-gas-wrapper",
            ("name", Loc.GetString(prototype.Name).ToLower())));

        if (prototype.Reagent != null)
        {
            var quantity = new Dictionary<string, FixedPoint2>
            {
                { prototype.Reagent, FixedPoint2.New(0.21f) }
            };
            var productLabel = ProductsLabel;
            SetReagents(quantity, ref productLabel, protoMan);
        }
        SetMixingCategory(categories, null, sysMan);
    }

    private void SetReagents(List<ReagentQuantity> reagents, ref RichTextLabel label, IPrototypeManager protoMan)
    {
        var amounts = new Dictionary<string, FixedPoint2>();
        foreach (var (reagent, quantity) in reagents)
        {
            amounts.Add(reagent.Prototype, quantity);
        }
        SetReagents(amounts, ref label, protoMan);
    }

    private void SetReagents(
        Dictionary<string, ReactantPrototype> reactants,
        ref RichTextLabel label,
        IPrototypeManager protoMan)
    {
        var amounts = new Dictionary<string, FixedPoint2>();
        foreach (var (reagent, reactantPrototype) in reactants)
        {
            amounts.Add(reagent, reactantPrototype.Amount);
        }
        SetReagents(amounts, ref label, protoMan);
    }

    [PublicAPI]
    private void SetReagents(
        Dictionary<ProtoId<MixingCategoryPrototype>, ReactantPrototype> reactants,
        ref RichTextLabel label,
        IPrototypeManager protoMan)
    {
        var amounts = new Dictionary<string, FixedPoint2>();
        foreach (var (reagent, reactantPrototype) in reactants)
        {
            amounts.Add(reagent, reactantPrototype.Amount);
        }
        SetReagents(amounts, ref label, protoMan);
    }

    private void SetReagents(Dictionary<string, FixedPoint2> reagents, ref RichTextLabel label, IPrototypeManager protoMan)
    {
        var msg = new FormattedMessage();
        var reagentCount = reagents.Count;
        var i = 0;
        foreach (var (product, amount) in reagents.OrderByDescending(p => p.Value))
        {
            msg.AddMarkupOrThrow(Loc.GetString("guidebook-reagent-recipes-reagent-display",
                ("reagent", protoMan.Index<ReagentPrototype>(product).LocalizedName), ("ratio", amount)));
            i++;
            if (i < reagentCount)
                msg.PushNewline();
        }
        msg.Pop();
        label.SetMessage(msg);
        label.Visible = true;
    }

    private void SetMixingCategory(IReadOnlyList<ProtoId<MixingCategoryPrototype>> mixingCategories, ReactionPrototype? prototype, IEntitySystemManager sysMan)
    {
        var foo = new List<MixingCategoryPrototype>();
        foreach (var cat in mixingCategories)
        {
            foo.Add(_protoMan.Index(cat));
        }
        SetMixingCategory(foo, prototype, sysMan);
    }

    private void SetMixingCategory(IReadOnlyList<MixingCategoryPrototype> mixingCategories, ReactionPrototype? prototype, IEntitySystemManager sysMan)
    {
        if (mixingCategories.Count == 0)
            return;

        // only use the first one for the icon.
        if (mixingCategories.First() is { } primaryCategory)
        {
            MixTexture.Texture = sysMan.GetEntitySystem<SpriteSystem>().Frame0(primaryCategory.Icon);
        }

        var mixingVerb = ContentLocalizationManager.FormatList(mixingCategories
            .Select(p => Loc.GetString(p.VerbText)).ToList());

        var minTemp = prototype?.MinimumTemperature ?? 0;
        var maxTemp = prototype?.MaximumTemperature ?? float.PositiveInfinity;
        var text = Loc.GetString("guidebook-reagent-recipes-mix-info",
            ("verb", mixingVerb),
            ("minTemp", minTemp),
            ("maxTemp", maxTemp),
            ("hasMax", !float.IsPositiveInfinity(maxTemp)));

        MixLabel.SetMarkup(text);
    }

    public bool CheckMatchesSearch(string query)
    {
        return this.ChildrenContainText(query);
    }

    public void SetHiddenState(bool state, string query)
    {
        Visible = CheckMatchesSearch(query) ? state : !state;
    }
}
