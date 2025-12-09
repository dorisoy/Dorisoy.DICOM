namespace Sinol.DicomViewer.Helpers;

internal sealed class NameToPageTypeConverter
{
    private static readonly Type[] PageTypes = Assembly
        .GetExecutingAssembly()
        .GetTypes()
        .Where(t => t.Namespace?.StartsWith("Sinol.DicomViewer.Views.Pages") ?? false)
        .ToArray();

    public static Type? Convert(string pageName)
    {
        pageName = pageName.Trim().ToLower() + "page";

        return PageTypes.FirstOrDefault(singlePageType =>
            singlePageType.Name.Equals(pageName, StringComparison.CurrentCultureIgnoreCase)
        );
    }
}
