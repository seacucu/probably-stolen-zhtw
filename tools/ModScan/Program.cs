// Usage: ModScan <game dir> <mod folder>
// Scans the mod folder's listed assemblies with the installed game's ModScanner and exits with 1 when the game
// would refuse the mod or ask the player to approve capabilities (findings the manifest does not declare, or
// rules the game is retiring), 0 when it would activate it without a prompt.
using System.Collections;
using System.Reflection;
using System.Xml.Linq;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: ModScan <game dir> <mod folder>");
    return 2;
}
string managed = Path.Combine(args[0], "Probably Stolen_Data", "Managed");
string modFolder = Path.GetFullPath(args[1]);

AppDomain.CurrentDomain.AssemblyResolve += (_, e) =>
{
    string path = Path.Combine(managed, new AssemblyName(e.Name).Name + ".dll");
    return File.Exists(path) ? Assembly.LoadFrom(path) : null;
};

var manifest = XDocument.Load(Path.Combine(modFolder, "manifest.xml")).Root!;
var assemblies = manifest.Element("Assemblies")!.Elements("Assembly").Select(e => e.Value.Trim()).ToList();
var declared = new HashSet<string>(manifest.Element("Capabilities")?.Elements("Capability").Select(e => e.Value.Trim())
    ?? Enumerable.Empty<string>());

var game = Assembly.LoadFrom(Path.Combine(managed, "Assembly-CSharp.dll"));
var scan = game.GetType("ModScanner")!.GetMethod("Scan", BindingFlags.Public | BindingFlags.Static)!;
object result = scan.Invoke(null, new object?[]
{
    assemblies.Select(a => Path.Combine(modFolder, a)).ToArray(),
    assemblies.Select(Path.GetFileNameWithoutExtension).ToArray(),
    modFolder,
    false,
})!;

object? Field(string name) => result.GetType().GetField(name)?.GetValue(result);
IEnumerable<object> List(string name) => (Field(name) as IEnumerable)?.Cast<object>() ?? Enumerable.Empty<object>();
string Text(object finding) => finding.ToString() ?? "";

var problems = new List<string>();
if (Field("Error") is string error)
    problems.Add("unreadable: " + error);
foreach (var flag in List("HardFlags"))
{
    // Outside the game the scanner does not know the game's own assemblies, so it reports every reference to one
    // as unlisted. A reference to an assembly the game ships is fine.
    string text = Text(flag);
    if (GetKind(flag) == "unlisted-reference" && ShippedByGame(text))
        continue;
    problems.Add("refused: " + text);
}
foreach (var finding in List("Findings"))
{
    if (!declared.Contains(GetKind(finding)))
        problems.Add("undeclared capability: " + Text(finding));
}
// Rules the game is retiring: still allowed, but the game asks the player to approve the mod.
foreach (var finding in List("Pending"))
    problems.Add("approval prompt: " + Text(finding));

Console.WriteLine($"Mod scan ({Path.GetFileName(modFolder)}, {Field("MethodsScanned")} methods): "
    + (problems.Count == 0 ? "clean" : $"{problems.Count} problem(s)"));
foreach (var problem in problems)
    Console.WriteLine("  " + problem);
return problems.Count == 0 ? 0 : 1;

static string GetKind(object finding) => finding.GetType().GetField("Kind")?.GetValue(finding) as string ?? "";

bool ShippedByGame(string text)
{
    const string marker = "references ";
    int start = text.IndexOf(marker, StringComparison.Ordinal);
    if (start < 0)
        return false;
    start += marker.Length;
    int end = text.IndexOf(',', start);
    string name = end < 0 ? text[start..] : text[start..end];
    return File.Exists(Path.Combine(managed, name.Trim() + ".dll"));
}
