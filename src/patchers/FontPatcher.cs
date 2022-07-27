using System.Linq;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace LuciferWithinUs_LocLib {
    [HarmonyPatch]
    public class FontPatcher {

        static TMP_FontAsset MyriadProRegular_SDF;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameManager))]
        [HarmonyPatch("Initialize")]
        static void GameManager_Initialize_Prefix() {
            var assembly = Assembly.GetExecutingAssembly();
            var resname = assembly.GetManifestResourceNames().Single((string str) => str.EndsWith("font.bundle"));
            var stream = assembly.GetManifestResourceStream(resname);
            var bundle = UnityEngine.AssetBundle.LoadFromStream(stream);
            MyriadProRegular_SDF = bundle.LoadAsset<TMP_FontAsset>("font");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TextMeshProUGUI))]
        [HarmonyPatch("LoadFontAsset")]
        static void TextMeshProUGUI_LoadFontAsset_Prefix(ref TMP_FontAsset ___m_fontAsset, ref Material ___m_sharedMaterial) {
            if (___m_fontAsset.name.Equals("MyriadPro-Regular SDF")) {
                ___m_sharedMaterial = null;
                ___m_fontAsset = MyriadProRegular_SDF;
            }
        }
    }
}
