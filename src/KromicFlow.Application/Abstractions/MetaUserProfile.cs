namespace KromicFlow.Application.Abstractions;

public sealed record MetaInstagramBusinessAccount(string PageId, string InstagramAccountId, string Username, string ProfilePicture);

public sealed record MetaUserProfile(
    string MetaUserId, 
    string Email, 
    string FullName, 
    string InstagramUserId, 
    string InstagramUsername, 
    string AccessToken,
    List<MetaInstagramBusinessAccount> InstagramAccounts
);
