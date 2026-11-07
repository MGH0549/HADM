namespace Hadm.Application.Abstractions;

public interface ITextLocalizer
{
    string Get(string key);
    string Get(string language, string key);
    string Format(string language, string key, params object[] args);
}
