using UnityEngine;

using static JanitorsCloset.JanitorsClosetLoader;


namespace JanitorsCloset
{
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    class FindTexturePathFromModel  : MonoBehaviour
    {

#if false

        private void Start()
        {
            Log.Info("FindTexturePathFromModel");
            GameDatabase.Instance.databaseModel.ForEach(m => Log.Info("URL: " + m.name));

            PrintTextureDependenciesOf("Squad/Parts/Command/mk1pod/model");
            PrintTextureDependenciesOf("Squad/Spaces/mk1PodCockpit/model");
        }
#endif

        public static string getModelURL(string name)
        {
            foreach (GameObject go in GameDatabase.Instance.databaseModel)
            {
               
                if (go.name.Contains("/" + name + "/"))
                {
                    Log.Info("Found URL: " + go.name);
                    return go.name;
                }
            }
            return "";
        }

#if false
        private static void PrintTextureDependenciesOf(string modelUrl)
        {
            var model = GameDatabase.Instance.GetModel(modelUrl);

            if (model == null) throw new ArgumentException("'" + modelUrl + "' not found");

            Log.Info("Dependencies of " + modelUrl + ":");

            foreach (var d in GetUrlsOfTextureDependencies(model))
                Log.Info("  Texture: " + d);

            Log.Info("End dependency list");
        }


        // returns URLs of all textures (that exist in GameDatabase) that the GO depends on
        private static IEnumerable<string> GetUrlsOfTextureDependencies(GameObject go)
        {
            var dependencies = new HashSet<string>();
            var texturesWithUrls = MatchTexturesToUrls(GetTexturesFromGameObject(go));

            foreach (var textureWithUrl in texturesWithUrls)
            {
                if (string.IsNullOrEmpty(textureWithUrl.Value))
                {
                    Log.Warning(textureWithUrl.Key.name + " not found in GameDatabase");
                    continue; // texture wasn't found in GameDatabase so no URL
                }

                dependencies.Add(textureWithUrl.Value);
            }

            return dependencies;
        }



        // (Texture, url in GameDatabase). If the texture wasn't found in GD, url will be empty
        private static IEnumerable<KeyValuePair<Texture, string>> MatchTexturesToUrls(IEnumerable<Texture> textures)
        {
            var matchedTextures = new List<KeyValuePair<Texture, string>>();

            foreach (var tex in textures)
            {
                if (string.IsNullOrEmpty(tex.name))
                    matchedTextures.Add(new KeyValuePair<Texture, string>(tex, string.Empty));
                else
                {
                    string textureUrl;

                    matchedTextures.Add(FindTextureInGameDatabase(tex, out textureUrl)
                        ? new KeyValuePair<Texture, string>(tex, textureUrl)
                        : new KeyValuePair<Texture, string>(tex, string.Empty));
                }
            }

            return matchedTextures;
        }


        // Searches for given texture. Returns true + its url if found in GameDatabase
        private static bool FindTextureInGameDatabase(Texture texture, out string textureUrl)
        {
            textureUrl = string.Empty;

            foreach (var dbTexture in GameDatabase.Instance.databaseTexture)
                if (ReferenceEquals(dbTexture.texture, texture))
                {
                    textureUrl = dbTexture.file.url;
                    return true;
                }

            return false;
        }


        private static IEnumerable<Texture> GetTexturesFromGameObject(GameObject go)
        {
            return go.GetComponentsInChildren<Renderer>(true)
                .Where(r => r.sharedMaterial != null)
                .Select(r => r.sharedMaterial)
                .SelectMany(GetTexturesFromMaterial)
                .Distinct();
        }


        // Material might have several different textures associated with it
        private static IEnumerable<Texture> GetTexturesFromMaterial(Material mat)
        {
            var texturePropertyNames = new[] { "_MainTex", "_BumpMap", "_SpecMap", "_Normal" };
            var textures = new List<Texture>();

            foreach (var property in texturePropertyNames)
            {
                Texture texture;

                if (GetTextureFromMaterial(mat, property, out texture))
                    textures.Add(texture);
            }

            return textures;
        }

#endif

        // Try to get a texture. The passed in material might not have such a property or it might not be set in
        // which case return false
        private static bool GetTextureFromMaterial(Material material, string propertyName, out Texture retTexture)
        {
            retTexture = null;

            if (!material.HasProperty(propertyName))
                return false;

            retTexture = material.GetTexture(propertyName);

            return retTexture != null;
        }
    }

}
