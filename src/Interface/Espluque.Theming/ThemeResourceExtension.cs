using System.ComponentModel;
using System.Windows;
using System.Windows.Markup;

namespace Espluque.Theming;

[MarkupExtensionReturnType(typeof(object))]
public sealed class ThemeResourceExtension : MarkupExtension
{
    private static readonly Lazy<ResourceDictionary> DesignResources = new(
        () => new ResourceDictionary
        {
            Source = new Uri(
                "pack://application:,,,/Espluque.Theming;component/Themes/Dark.xaml",
                UriKind.Absolute)
        });

    public ThemeResourceExtension(object resourceKey)
    {
        ResourceKey = resourceKey;
    }

    [ConstructorArgument("resourceKey")]
    public object ResourceKey { get; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (IsInDesignMode(serviceProvider))
        {
            return DesignResources.Value[ResourceKey]
                ?? throw new InvalidOperationException(
                    $"Ressource de thème introuvable : {ResourceKey}");
        }

        return new DynamicResourceExtension(ResourceKey)
            .ProvideValue(serviceProvider);
    }

    private static bool IsInDesignMode(IServiceProvider serviceProvider)
    {
        if (serviceProvider.GetService(typeof(IProvideValueTarget))
                is IProvideValueTarget
            {
                TargetObject: DependencyObject target
            })
        {
            return DesignerProperties.GetIsInDesignMode(target);
        }

        return (bool)DesignerProperties.IsInDesignModeProperty
            .GetMetadata(typeof(DependencyObject))
            .DefaultValue;
    }
}