using lab6;
using Microsoft.Win32;

#pragma warning disable CA1416 // Validate platform compatibility
Console.WriteLine("Find registry entries by key or value:\n/k <keyName> or /v <value>");
var input = Console.ReadLine();
var argList = input!.Split(' ');
List<RegistryKey> keys;

if (argList[0] == "/v")
{
    keys = RegistryHelper.FindKeysByValue(argList[1]);
}
else if (argList[0] == "/k")
{
    keys = RegistryHelper.FindKeysByName(argList[1]);
}
else
{
    return;
}


if (keys.Count == 0)
{
    return;
}

foreach (var key in keys)
{
    Console.WriteLine(key);
}

var writeFile = YNQuestion("Would you like to write those keys to reg file?");

if (writeFile)
{
    var keysRep = string.Empty;
    foreach (var key in keys)
    {
        keysRep += RegistryHelper.GetRegistryKeyRep(key);
    }

    RegistryHelper.WriteKeyRepToRegFile("RegOut.reg", keysRep);
}
#pragma warning restore CA1416 // Validate platform compatibility


static bool YNQuestion(string prompt)
{
    Console.WriteLine($"{prompt}(y/n)");
    var ans = Console.ReadLine()?.ToLower();
    if (ans == "y" || ans == "yes")
    {
        return true;
    }
    return false;
}
