﻿using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using Microsoft.Win32;

string? bannerlordPath = ResolveBannerlordPath();
if (bannerlordPath == null)
{
    Console.WriteLine("Could not find the location of your Bannerlord installation. Contact a moderator on discord. Press enter to exit.");
    Console.Read();
    return;
}

Console.WriteLine($"Using Bannerlord installed at '{bannerlordPath}'");

try
{
    await UpdateCrpgAsync(bannerlordPath);
}
catch (Exception e)
{
    Console.WriteLine("Could not update cRPG. The game will still launch but you might get a version mismatch error. Press enter to continue.");
    Console.WriteLine(e);
    Console.Read();
}

string bannerlordExePath = ResolveBannerlordExePath(bannerlordPath)!;
Process.Start(new ProcessStartInfo
{
    WorkingDirectory = Path.GetDirectoryName(bannerlordExePath),
    FileName = Path.GetFileName(bannerlordExePath),
    Arguments = "_MODULES_*Native*cRPG*_MODULES_ /multiplayer", // For Xbox it will run the launcher and those args are getting ignored.
    UseShellExecute = true,
});

static string? ResolveBannerlordPath()
{
    string? bannerlordPath = ResolveBannerlordPathSteam();
    if (bannerlordPath != null)
    {
        return bannerlordPath;
    }

    bannerlordPath = ResolveBannerlordPathEpicGames();
    if (bannerlordPath != null)
    {
        return bannerlordPath;
    }

    bannerlordPath = ResolveBannerlordPathXbox();
    if (bannerlordPath != null)
    {
        return bannerlordPath;
    }

    string bannerlordPathFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "Mount and Blade II Bannerlord/cRPG/BannerlordPath.txt");
    if (File.Exists(bannerlordPathFile))
    {
        bannerlordPath = File.ReadAllText(bannerlordPathFile);
        if (File.Exists(bannerlordPath))
        {
            return bannerlordPath;
        }
    }

    bannerlordPath = AskForBannerlordPath();
    if (bannerlordPath != null)
    {
        Directory.CreateDirectory(Directory.GetParent(bannerlordPathFile)!.FullName);
        File.WriteAllText(bannerlordPathFile, bannerlordPath);
        return bannerlordPath;
    }

    return null;
}

static string? ResolveBannerlordPathSteam()
{
    string? steamPath = (string?)Registry.GetValue("HKEY_CURRENT_USER\\Software\\Valve\\Steam", "SteamPath", null);
    if (steamPath == null)
    {
        return null;
    }

    string vdfPath = Path.Combine(steamPath, "steamapps/libraryfolders.vdf");
    if (!File.Exists(vdfPath))
    {
        return null;
    }

    VProperty vdf = VdfConvert.Deserialize(File.ReadAllText(vdfPath));

    for (int i = 0; ; i += 1)
    {
        string index = i.ToString();
        if (vdf.Value[index] == null)
        {
            break;
        }

        string? path = vdf.Value[index]?["path"]?.ToString();
        if (path == null)
        {
            continue;
        }

        string bannerlordPath = Path.Combine(path, "steamapps/common/Mount & Blade II Bannerlord");
        if (ResolveBannerlordExePath(bannerlordPath) != null)
        {
            return bannerlordPath;
        }
    }

    return null;
}

static string? ResolveBannerlordPathEpicGames()
{
    const string defaultBannerlordPath = "C:/Program Files/Epic Games/Chickadee";
    if (ResolveBannerlordExePath(defaultBannerlordPath) != null)
    {
        return defaultBannerlordPath;
    }

    return null;
}

static string? ResolveBannerlordPathXbox()
{
    const string defaultBannerlordPath = "C:/XboxGames/Mount & Blade II- Bannerlord/Content";
    if (ResolveBannerlordExePath(defaultBannerlordPath) != null)
    {
        return defaultBannerlordPath;
    }

    return null;
}

static string? AskForBannerlordPath()
{
    string? bannerlordPath = null;
    while (bannerlordPath == null)
    {
        Console.WriteLine("Enter your Mount & Blade II Bannerlord location (e.g. D:\\Steam\\steamapps\\common\\Mount & Blade II Bannerlord):");
        bannerlordPath = Console.ReadLine();
        if (bannerlordPath == null)
        {
            break;
        }

        bannerlordPath = bannerlordPath.Trim();
        if (ResolveBannerlordExePath(bannerlordPath) == null)
        {
            Console.WriteLine($"Could not find Bannerlord at '{bannerlordPath}'");
            bannerlordPath = null;
        }
    }

    return bannerlordPath;
}

static string? ResolveBannerlordExePath(string bannerlordPath)
{
    string bannerlordExePath = Path.Combine(bannerlordPath, "bin/Win64_Shipping_Client/Bannerlord.exe");
    if (File.Exists(bannerlordExePath))
    {
        return bannerlordExePath;
    }

    bannerlordExePath = Path.Combine(bannerlordPath, "bin/Gaming.Desktop.x64_Shipping_Client/Launcher.Native.exe");
    if (File.Exists(bannerlordExePath))
    {
        return bannerlordExePath;
    }


    return null;
}

static async Task UpdateCrpgAsync(string bannerlordPath)
{
    string crpgPath = Path.Combine(bannerlordPath, "Modules/cRPG");
    string tagPath = Path.Combine(crpgPath, "Tag.txt");
    string? tag = File.Exists(tagPath) ? File.ReadAllText(tagPath) : null;

    using HttpClient httpClient = new();
    HttpRequestMessage req = new(HttpMethod.Get, "https://c-rpg.eu/cRPG.zip");
    if (tag != null)
    {
        req.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(tag));
    }

    var res = await httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
    if (res.StatusCode == HttpStatusCode.NotModified)
    {
        return;
    }

    res.EnsureSuccessStatusCode();

    long contentLength = res.Content.Headers.ContentLength!.Value;
    await using var contentStream = await res.Content.ReadAsStreamAsync();
    using MemoryStream ms = await DownloadWithProgressBarAsync(contentStream, contentLength);

    using (ZipArchive archive = new(ms))
    {
        if (Directory.Exists(crpgPath))
        {
            Directory.Delete(crpgPath, true);
        }

        Directory.CreateDirectory(crpgPath);
        archive.ExtractToDirectory(crpgPath); // No async overload :(
    }

    tag = res.Headers.ETag?.Tag;
    if (tag != null)
    {
        File.WriteAllText(tagPath, tag);
    }
}

static async Task<MemoryStream> DownloadWithProgressBarAsync(Stream stream, long length)
{
    MemoryStream ms = new();
    byte[] buffer = new byte[100 * 1000];
    int totalBytesRead = 0;
    int bytesRead;
    while ((bytesRead = await stream.ReadAsync(buffer)) != 0)
    {
        ms.Write(buffer, 0, bytesRead);

        totalBytesRead += bytesRead;
        float progression = (float)Math.Round(100 * (float)totalBytesRead / length, 2);
        string lengthStr = length.ToString();
        string totalBytesReadStr = totalBytesRead.ToString().PadLeft(lengthStr.Length);
        Console.WriteLine($"Downloading {totalBytesReadStr} / {lengthStr} ({progression}%)");
    }

    return ms;
}
