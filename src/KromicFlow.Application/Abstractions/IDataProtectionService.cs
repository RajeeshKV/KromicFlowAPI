namespace KromicFlow.Application.Abstractions;

public interface IDataProtectionService
{
    string Protect(string plaintext);
    string Unprotect(string protectedText);
}
