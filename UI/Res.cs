using System.Reflection;
using System.Resources;

namespace DuelLedger.UI;

internal static class Res
{
    private static readonly ResourceManager Manager = new("DuelLedger.UI.Resources.Strings", Assembly.GetExecutingAssembly());
    public static string Get(string name) => Manager.GetString(name) ?? string.Empty;
}
