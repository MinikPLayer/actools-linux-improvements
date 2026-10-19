using System.Reflection;
using System.Text.Json;
// usage: apidump <out.json> <dir-or-dll>...   -> { "Ns.Type": { "b": "base full name", "p": [props], "e": [events], "ns": "namespace", "asm": "assembly" } }
var outFile = args[0];
var inputs = args.Skip(1).ToList();
var files = new List<string>();
foreach (var i in inputs) { if (Directory.Exists(i)) files.AddRange(Directory.GetFiles(i, "*.dll")); else files.Add(i); }
var refDirs = new[] { "/usr/lib/dotnet/shared/Microsoft.NETCore.App/10.0.12" };
var all = files.Concat(refDirs.SelectMany(d => Directory.GetFiles(d, "*.dll"))).GroupBy(Path.GetFileName).Select(g => g.First()).ToList();
// dependencies of project assemblies live next to them
foreach (var d in inputs.Where(File.Exists).Select(Path.GetDirectoryName).Distinct()) all.AddRange(Directory.GetFiles(d!, "*.dll").Where(f => !all.Any(a => Path.GetFileName(a) == Path.GetFileName(f))));
var resolver = new PathAssemblyResolver(all);
using var mlc = new MetadataLoadContext(resolver);
var result = new Dictionary<string, object>();
foreach (var f in files) {
    Assembly asm; try { asm = mlc.LoadFromAssemblyPath(f); } catch { continue; }
    Type[] types; try { types = asm.GetExportedTypes(); } catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray()!; } catch { continue; }
    foreach (var t in types) {
        if (!t.IsClass || t.IsGenericTypeDefinition) continue;
        var props = new SortedSet<string>();
        foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)) props.Add(p.Name);
        foreach (var fl in t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)) if (fl.Name.EndsWith("Property")) props.Add(fl.Name[..^8]);
        var events = new SortedSet<string>();
        foreach (var e in t.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)) events.Add(e.Name);
        string? b = null; try { b = t.BaseType?.FullName; } catch { }
        result[t.FullName!] = new { b, p = props, e = events, ns = t.Namespace, asm = asm.GetName().Name };
    }
}
File.WriteAllText(outFile, JsonSerializer.Serialize(result));
Console.WriteLine($"{result.Count} types");
