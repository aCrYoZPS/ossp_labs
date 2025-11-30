using System.Runtime.Versioning;
using System.Text;
using Microsoft.Win32;

namespace lab6;

[SupportedOSPlatform("Windows")]
public class RegistryHelper
{
    private static HashSet<string> seenKeys;
    private static List<RegistryKey> foundKeys;


    static IEnumerable<string> SplitIntoChunks(string str, int chunkSize)
    {
        return Enumerable.Range(0, str.Length / chunkSize)
            .Select(i => str.Substring(i * chunkSize, chunkSize));
    }

    public static string RegfileStringifyRegistryString(string str)
    {
        return $"\"{str.Replace("\\", "\\\\")}\"";
    }

    public static string RegfileStringifyRegistryBinary(byte[] bytes)
    {
        return $"hex:{string.Join(',', bytes.Select(b => b.ToString("x2")))}";
    }

    public static string RegfileStringifyRegistryDWord(int dword)
    {
        return $"dword:{dword:x8}";
    }

    public static string RegfileStringifyRegistryQWord(long qword)
    {
        var byteChunks = SplitIntoChunks($"{qword:x}", 2);
        return $"hex(b):{string.Join(',', byteChunks.Reverse())}";
    }

    public static string RegfileStringifyRegistryMultiString(string[] strings)
    {
        var byteRepresentations = strings.Select(s => s.Append('\0')
                                                       .Select(c => SplitIntoChunks(((short)c).ToString("x4"), 2).Reverse())
                                                       .SelectMany(i => i))
                                         .SelectMany(i => i);
        byteRepresentations = byteRepresentations.Concat(["00", "00"]);
        var res = "hex(7):";
        var printedBytes = 0;
        foreach (var byteRep in byteRepresentations)
        {
            res += $"{byteRep},";
            printedBytes += 1;
            if (printedBytes >= 10)
            {
                printedBytes = 0;
                res += "\\\n  ";
            }
        }

        return res.Trim(',');
    }

    public static string RegfileStringifyRegistryExpandableString(string expandableString)
    {
        return $"hex(2):{string.Join(',', expandableString.Append('\0').Select(c => SplitIntoChunks(((short)c).ToString("x4"), 2).Reverse()).SelectMany(i => i))}";
    }

    public static string GetRegfileStringRepresentation(object? registryValue, RegistryValueKind kind)
    {
        if (registryValue is null)
        {
            return "NULL";
        }

        return kind switch
        {
            RegistryValueKind.String => RegfileStringifyRegistryString((string)registryValue),
            RegistryValueKind.Binary => RegfileStringifyRegistryBinary((byte[])registryValue),
            RegistryValueKind.DWord => RegfileStringifyRegistryDWord((int)registryValue),
            RegistryValueKind.QWord => RegfileStringifyRegistryQWord((long)registryValue),
            RegistryValueKind.MultiString => RegfileStringifyRegistryMultiString((string[])registryValue),
            RegistryValueKind.ExpandString => RegfileStringifyRegistryExpandableString((string)registryValue),
            RegistryValueKind.Unknown => "Value of unknown type (what?)",
            RegistryValueKind.None => "Value of none type (what?)",
            _ => throw new Exception("Unknown type, aborting")
        };
    }

    public static string ReadableStringifyRegistryBinary(byte[] bytes)
    {
        return string.Join(' ', bytes.Select(b => b.ToString("x2")));
    }

    public static string ReadableStringifyDWord(object dword)
    {
        if (int.TryParse(dword.ToString(), out var result))
        {
            return $"0x{result:x8}";
        }
        else
        {
            return dword.ToString()!;
        }
    }

    public static string GetReadableRegistryValueStringRepresentation(object? registryValue, RegistryValueKind kind)
    {
        if (registryValue is null)
        {
            return "NULL";
        }

        return kind switch
        {
            RegistryValueKind.String => (string)registryValue,
            RegistryValueKind.Binary => ReadableStringifyRegistryBinary((byte[])registryValue),
            // RegistryValueKind.DWord => $"0x{(int)registryValue:x8}",
            RegistryValueKind.DWord => ReadableStringifyDWord(registryValue),
            RegistryValueKind.QWord => $"0x{(long)registryValue:x16}",
            RegistryValueKind.MultiString => $"[{string.Join(',', (string[])registryValue)}]",
            RegistryValueKind.ExpandString => (string)registryValue,
            RegistryValueKind.Unknown => "Value of unknown type (what?)",
            RegistryValueKind.None => "Value of none type (what?)",
            _ => throw new Exception("Unknown type, aborting")
        };
    }

    public static string GetRegistryKeyRep(RegistryKey key, bool tree = true)
    {
        var res = $"[{key}]\n";
        var valueNames = key.GetValueNames();
        for (int i = 0; i < valueNames.Length; ++i)
        {
            var valueName = valueNames[i];
            var value = GetRegfileStringRepresentation(key.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames), key.GetValueKind(valueName));

            if (valueName == string.Empty)
            {
                valueName = "@";
            }
            else
            {
                valueName = $"\"{valueName}\"";
            }

            res += $"{valueName}={value}\n";
        }

        if (!tree)
        {
            return res;
        }

        var subKeys = key.GetSubKeyNames();
        foreach (var subKeyName in subKeys)
        {
            var subKey = key.OpenSubKey(subKeyName);
            if (subKey is not null)
            {
                res += $"\n{GetRegistryKeyRep(subKey, tree)}";
            }
        }

        return res;
    }

    public static void WriteKeyRepToRegFile(string filePath, string keyRep)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        var bytes = Encoding.UTF8.GetBytes($"Windows Registry Editor Version 5.00\n\n{keyRep}");
        using var file = File.OpenWrite(filePath);
        file.Write(bytes);
    }


    public static List<RegistryKey> FindKeysByName(string keyName, RegistryKey? parentKey = null)
    {
        foundKeys = [];
        seenKeys = [];

        FindKeysByNameInternal(keyName, parentKey);

        var foundKeysCopy = foundKeys.ToList();

        foundKeys = [];
        seenKeys = [];

        return foundKeysCopy;
    }

    private static void FindKeysByNameInternal(string keyName, RegistryKey? parentKey = null)
    {
        var searchRootKey = parentKey ?? Registry.CurrentUser; // For run time sake

        if (searchRootKey.Name.Split('\\')[^1] == keyName)
        {
            if (seenKeys.Add(searchRootKey.Name))
            {
                foundKeys.Add(searchRootKey);
            }

            return;
        }

        var subKeys = searchRootKey.GetSubKeyNames();
        if (subKeys.Contains(keyName))
        {
            var foundKey = searchRootKey.OpenSubKey(keyName)!;
            if (seenKeys.Add(foundKey.Name))
            {
                foundKeys.Add(searchRootKey.OpenSubKey(keyName)!);
            }

            return;
        }

        foreach (var subKeyName in subKeys)
        {
            FindKeysByNameInternal(keyName, searchRootKey.OpenSubKey(subKeyName)!);
        }
    }


    public static List<RegistryKey> FindKeysByValue(string searchValue, RegistryKey? parentKey = null)
    {
        foundKeys = [];
        seenKeys = [];

        FindKeysByValueInternal(searchValue, parentKey);

        var foundKeysCopy = foundKeys.ToList();

        foundKeys = [];
        seenKeys = [];

        return foundKeysCopy;
    }

    private static void FindKeysByValueInternal(string searchValue, RegistryKey? parentKey = null)
    {
        var searchRootKey = parentKey ?? Registry.CurrentUser; // For run time sake

        var valueNames = searchRootKey.GetValueNames();
        for (int i = 0; i < valueNames.Length; ++i)
        {
            var valueName = valueNames[i];
            var value = GetReadableRegistryValueStringRepresentation(
                searchRootKey.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames),
                searchRootKey.GetValueKind(valueName)
            );

            if (value == searchValue)
            {
                if (seenKeys.Add(searchRootKey.Name))
                {
                    foundKeys.Add(searchRootKey);
                }

                return;
            }
        }

        var subKeys = searchRootKey.GetSubKeyNames();

        foreach (var subKeyName in subKeys)
        {
            FindKeysByValueInternal(searchValue, searchRootKey.OpenSubKey(subKeyName)!);
        }
    }
}
