// Sets the tags of the Workshop item in workshop/publishedfileid.txt. Run it while the Steam client is
// signed in as the item's owner:
//   dotnet run --project tools\WorkshopTags -- Mod Translation
using Steamworks;

const uint AppId = 4348910;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: WorkshopTags <tag> [<tag> ...]");
    return 2;
}

string idFile = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "workshop", "publishedfileid.txt");
var item = new PublishedFileId_t(ulong.Parse(File.ReadAllText(idFile).Trim()));

// SteamAPI.Init reads the app id from this variable when it is not started by Steam.
Environment.SetEnvironmentVariable("SteamAppId", AppId.ToString());
if (!SteamAPI.Init())
{
    Console.Error.WriteLine("SteamAPI.Init failed: is the Steam client running and signed in?");
    return 1;
}

try
{
    Console.WriteLine($"Signed in as {SteamFriends.GetPersonaName()}; setting tags of {item} to: {string.Join(", ", args)}");
    var update = SteamUGC.StartItemUpdate(new AppId_t(AppId), item);
    SteamUGC.SetItemTags(update, args.ToList());

    SubmitItemUpdateResult_t? result = null;
    using var call = CallResult<SubmitItemUpdateResult_t>.Create((r, ioFailure) => result = ioFailure ? null : r);
    call.Set(SteamUGC.SubmitItemUpdate(update, null));
    var deadline = DateTime.UtcNow.AddMinutes(1);
    while (result == null && DateTime.UtcNow < deadline)
    {
        SteamAPI.RunCallbacks();
        Thread.Sleep(100);
    }

    if (result?.m_eResult != EResult.k_EResultOK)
    {
        Console.Error.WriteLine($"Update failed: {result?.m_eResult.ToString() ?? "no answer from Steam"}");
        return 1;
    }
    Console.WriteLine("Done.");
    return 0;
}
finally
{
    SteamAPI.Shutdown();
}
