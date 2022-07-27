using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using I2.Loc;

namespace LuciferWithinUs_LocLib {
    [HarmonyPatch]
    public class TextPatcher {
        class LangData {
            public class Meta {
                public string code;
                public string name;
                public string displayName;
            }
            public Meta meta;
            public Dictionary<string, string> data;
        }

        static List<LangData.Meta> additionalLangs = new List<LangData.Meta>();

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LocalizationManager))]
        [HarmonyPatch("AddSource")]
        static bool LocalizationManager_AddSource_Prefix(LanguageSourceData Source) {
            if (LocalizationManager.Sources.Contains(Source)) {
                return false;
            }

            var languagesDir = Directory.CreateDirectory(Path.Combine(BepInEx.Paths.BepInExRootPath, "languages"));
            languagesDir.EnumerateFiles().Do((FileInfo file) => {
                var langData = JSONParser.LoadJSONFile<LangData>(file.FullName);

                var lang = new LanguageData();
                lang.Code = langData.meta.code;
                lang.Name = langData.meta.name;
                Source.mLanguages.Add(lang);

                additionalLangs.Add(langData.meta);

                Plugin.Instance.log.LogInfo("LocalizationManager:AddSource loaded file " + file.Name);
                Source.mTerms.Do(term => {
                    if (langData.data.ContainsKey(term.Term)) {
                        term.Languages = term.Languages.AddToArray(langData.data[term.Term]);
                        langData.data.Remove(term.Term);
                    } else {
                        term.Languages = term.Languages.AddToArray("[!]" + term.Languages[0]);
                        langData.data.Remove(term.Term);

                        Plugin.Instance.log.LogError("LocalizationManager:AddSource missing key: " + term.Term + "!");
                    }
                });
                langData.data.Do(pair => {
                    Plugin.Instance.log.LogWarning("LocalizationManager:AddSource unknown key: " + pair.Key + "!");
                });
            });

            return true;
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(SettingsController))]
        [HarmonyPatch("Initialize")]
        static void SettingsController_Initialize_Prefix(ref string[] ___languageDisplayNames) {
            ___languageDisplayNames = ___languageDisplayNames.AddRangeToArray(additionalLangs.Select(meta => meta.displayName).ToArray());
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SettingsController))]
        [HarmonyPatch("ConvertLanguageNametoID")]
        static bool SettingsController_ConvertLanguageNametoID_Prefix(ref string __result, string target) {
            var lagMeta = additionalLangs.Find(meta => meta.displayName == target);
            if (lagMeta != null) {
                __result = lagMeta.name;
                return false;
            };
            return true;
        }

    }
}
