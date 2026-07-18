namespace KromicFlow.Application.Abstractions;

public interface IOAuthStateService
{
    string GenerateState();
    bool ValidateState(string state);
}
