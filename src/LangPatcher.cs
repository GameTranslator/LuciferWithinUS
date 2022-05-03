using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System.Text.Json;

public class LangPatcher {
    private string rootPath;

    public LangPatcher(string rootPath) {
        this.rootPath = rootPath;
    }

    public void Run() {
        var dataPath = Path.Combine(rootPath, "LuciferWithinUs_Data");
        var assetsPath = Path.Combine(dataPath, "resources.assets");
        var assetsTmpPath = Path.Combine(dataPath, "resources-modified.assets");
        var assetsBakPath = assetsPath + ".bak";

        var am = new AssetsManager();
        am.LoadClassPackage("classdata.tpk");
        var inst = am.LoadAssetsFile(assetsPath, false);
        var managedPath = Path.Combine(Path.GetDirectoryName(inst.path), "Managed");
        inst.table.GenerateQuickLookupTree();
        am.LoadClassDatabaseFromPackage(inst.file.typeTree.unityVersion);

        bool patched = false;
        var replacers = new List<AssetsReplacer>();

        foreach (var inf in inst.table.GetAssetsOfType((int)AssetClassID.MonoBehaviour)) {
            var baseField = am.GetTypeInstance(inst, inf).GetBaseField();

            var name = baseField.Get("m_Name").IsDummy() ? "" : baseField.Get("m_Name").GetValue().AsString();
            if (name == "I2Languages") {
                var monoBehaviour = MonoDeserializer.GetMonoBaseField(am, inst, inf, managedPath);
                var mTerms = monoBehaviour.Get("mSource").Get("mTerms");
                var termsMap = mTerms.GetChildrenList().ToLookup((f) => f.Get("Term").GetValue().AsString());

                doMapping(ref termsMap);

                mTerms.SetChildrenList(termsMap.Select((g) => g.First()).ToArray());
                var changedBytes = monoBehaviour.WriteToByteArray();
                var replacer = new AssetsReplacerFromMemory(0, inf.index, (int)inf.curFileType, AssetHelper.GetScriptIndex(inst.file, inf), changedBytes);
                replacers.Add(replacer);
                patched = true;
                break;
            }
        }

        if (patched) {
            using (var stream = File.OpenWrite(assetsTmpPath))
            using (var writer = new AssetsFileWriter(stream)) {
                inst.file.Write(writer, 0, replacers, 0);
            }
            am.UnloadAllAssetsFiles();

            if (!File.Exists(assetsBakPath)) {
                File.Move(assetsPath, assetsBakPath, true);
            }
            File.Move(assetsTmpPath, assetsPath, true);
            Console.WriteLine("Patched!");
        } else {
            am.UnloadAllAssetsFiles();

            Console.WriteLine("Not patched!");
        }
        Console.WriteLine("Enter any key to continue...");
        Console.ReadKey();
    }

    private static bool doMapping(ref ILookup<string, AssetTypeValueField> termsMap) {
        var found = false;
        var localTermsMap = termsMap;
        Array.ForEach(new DirectoryInfo(Directory.GetCurrentDirectory()).GetFiles("*.json"), (f) => {
            found = true;
            var path = f.FullName;
            var prefix = Path.GetFileNameWithoutExtension(path);
            JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path)).ToList().ForEach((kv) => {
                localTermsMap[kv.Key].First().Get("Languages")
                    .GetChildrenList()[0].GetValue().Set(kv.Value);
            });
        });
        return found;
    }
}
