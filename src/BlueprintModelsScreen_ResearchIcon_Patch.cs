using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace ResearchAvailable
{

    [HarmonyPatch(typeof(BlueprintModelsScreen), "UpdateResearchIcon")]    
    public static class BlueprintModelsScreen_ResearchIcon_Patch
    {
        /// <summary>
        //  The Research Icon game objects.  They are a 1 to 1 index match with the game's tab groups.
        /// </summary>
        public static List<GameObject> ResearchIconObjects;

        /// <summary>
        /// Hack to detect if the game has been reloaded.
        /// </summary>
        public static WeakReference<GameObject> FirstTabObject;

        public static void Postfix(BlueprintModelsScreen __instance, List<IndexButton> ___TabButtons, GameManager ___GM)
        {

            try
            {
                //Exit if the tabs haven't been inited yet.
                if (__instance?.BlueprintTabs?.Length == 0 || ___TabButtons?.Count == 0)
                {
                    return;
                }

                List<GameObject> researchIconObjects = BlueprintModelsScreen_ResearchIcon_Patch.ResearchIconObjects;


                GameObject firstTabObject = null;

                //Check if the blueprint objects have changed.  Should be when the player exits to the main menu
                //  and then load a game.
                //Check if the tab buttons have been inited by the game.s
                if(FirstTabObject == null || FirstTabObject.TryGetTarget(out firstTabObject) == false || 
                    firstTabObject != ___TabButtons.FirstOrDefault().gameObject)
                {
                    researchIconObjects = new List<GameObject>();

                    //Get the icon's file name
                    string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    Texture2D texture = LoadPNG(Path.Combine(assemblyPath, "ResearchIcon.png"));

                    Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), Vector2.zero);

                    for (int index = 0; index < ___TabButtons.Count; index++)
                    {
                        GameObject gameObject = new GameObject();
                        gameObject.name = "Available Research Icon";
                        Image image = gameObject.AddComponent<Image>();
                        image.sprite = sprite;

                        gameObject.GetComponent<RectTransform>().SetParent(___TabButtons[index].transform);
                        gameObject.transform.localScale = Vector3.one;
                        gameObject.transform.position = Vector3.zero;
                        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
                        rectTransform.sizeDelta = new Vector2(texture.width, texture.height);
                        //Don't know why, but this is the magic offset.  Zero is the center of the tab.
                        rectTransform.localPosition = new Vector3(-64, 27, 0);

                        researchIconObjects.Add(gameObject);
                    }

                    FirstTabObject = new WeakReference<GameObject>(___TabButtons[0].gameObject);

                    BlueprintModelsScreen_ResearchIcon_Patch.ResearchIconObjects = researchIconObjects;
                }

                //This should no longer happen.  Keeping in case something changes.
                if(__instance?.BlueprintTabs?.Length != ResearchIconObjects?.Count)
                {
                    Plugin.Log.LogError($"Count mismatch: {__instance?.BlueprintTabs?.Length} ResearchIconObjects?.Count {ResearchIconObjects?.Count}");
                }
                else
                {
                    for (int i = 0; i < __instance.BlueprintTabs.Length; i++)
                    {
                        CardTabGroup group = __instance.BlueprintTabs[i];

                        ResearchIconObjects[i].SetActive(group != null && HasResearchable(__instance, ___GM, group));
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Error updating the research screen ___TabButtons?.Count: {___TabButtons?.Count} ResearchIconObjects?.Count:  {ResearchIconObjects?.Count} ");

                Plugin.Log.LogError(ex.ToString());
            }
        }

        public static Texture2D LoadPNG(string filePath)
        {
            Texture2D tex = null;
            byte[] fileData;

            if (File.Exists(filePath))
            {
                fileData = File.ReadAllBytes(filePath);
                tex = new Texture2D(2, 2);
                tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
            }
            else
            {
                throw new FileNotFoundException($"Unable to find {filePath}");
            }

            return tex;
        }

        /// <summary>
        /// Returns true if the tab has at least one researchable card.  Excludes the card currently being researched.
        /// </summary>
        /// <param name="blueprintScreen"></param>
        /// <param name="gameManager"></param>
        /// <param name="tabGroup"></param>
        /// <returns></returns>
        private static bool HasResearchable(BlueprintModelsScreen blueprintScreen, GameManager gameManager, CardTabGroup tabGroup)
        {
            //Search all sub tabs for the current tab.
            return tabGroup.SubGroups.Any(group => group.IncludedCards != null &&
                group.IncludedCards.Any(card => card != null && blueprintScreen.CurrentResearch != card &&
                    gameManager.PurchasableBlueprintCards.Contains(card)));
        }

        
    }
}
