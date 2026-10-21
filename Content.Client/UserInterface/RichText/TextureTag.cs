using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.RichText;

/// <summary>
/// Renders the inline texture tag used by formatted radio headers.
/// RSI states are resolved through SpriteSystem, like Sunrise's BaseTextureTag.
/// </summary>
[UsedImplicitly]
public sealed partial class TextureTag : IMarkupTagHandler
{
    [Dependency] private IResourceCache _resourceCache = default!;
    [Dependency] private IEntitySystemManager _entitySystemManager = default!;

    private SpriteSystem? _spriteSystem;

    public string Name => "texture";

    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        control = null;

        if (!node.Attributes.TryGetValue("path", out var pathParam) ||
            !pathParam.TryGetString(out var path) ||
            string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var textureRect = new TextureRect();

        // Sunrise job icons are RSI states, represented as path.rsi/state.png.
        // They must be resolved by SpriteSystem rather than loaded as standalone PNG resources.
        if (path.Contains(".rsi/", StringComparison.OrdinalIgnoreCase))
        {
            var lastSlash = path.LastIndexOf('/');
            var lastDot = path.LastIndexOf('.');
            if (lastSlash < 0 || lastDot <= lastSlash)
                return false;

            var rsiPath = path[..lastSlash];
            var state = path[(lastSlash + 1)..lastDot];
            _spriteSystem ??= _entitySystemManager.GetEntitySystem<SpriteSystem>();
            textureRect.Texture = _spriteSystem.Frame0(new SpriteSpecifier.Rsi(new ResPath(rsiPath), state));
        }
        else
        {
            if (!_resourceCache.TryGetResource<TextureResource>(new ResPath(path), out var textureResource) ||
                textureResource == null)
            {
                return false;
            }

            textureRect.Texture = textureResource.Texture;
        }

        var scale = 1f;
        if (node.Attributes.TryGetValue("scale", out var scaleParam) &&
            scaleParam.TryGetString(out var scaleString) &&
            float.TryParse(scaleString, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedScale))
        {
            scale = Math.Clamp(parsedScale, 1f, 4f);
        }

        textureRect.TextureScale = new Vector2(scale, scale);
        textureRect.VerticalAlignment = Control.VAlignment.Center;
        control = textureRect;
        return true;
    }
}
