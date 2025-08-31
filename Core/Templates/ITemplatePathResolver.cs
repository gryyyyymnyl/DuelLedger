namespace DuelLedger.Core.Templates;

public interface ITemplatePathResolver
{
    string Get(string gameName);
}
