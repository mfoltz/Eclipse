using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TMPro;
using UnityEngine;
using StringComparison = System.StringComparison;

namespace Eclipse.Utilities;
internal static class GameObjects
{
    public static TMP_SpriteAsset CreateSpriteAsset(Sprite sprite)
    {
        // Create the TMP_SpriteAsset ScriptableObject
        var spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
        spriteAsset.name = sprite.name;

        // Assign the sprite's texture as the atlas
        spriteAsset.spriteSheet = sprite.texture;

        // Set up the sprite info list with just this one sprite
        spriteAsset.spriteInfoList.Clear();

        var info = new TMP_Sprite()
        {
            id = 0,
            name = sprite.name,
            hashCode = TMP_TextUtilities.GetSimpleHashCode(sprite.name),
            sprite = sprite,
            unicode = 0xE000,
            x = sprite.rect.x,
            y = sprite.rect.y,
            width = sprite.rect.width,
            height = sprite.rect.height,
            pivot = sprite.pivot / sprite.rect.size
        };

        spriteAsset.spriteInfoList.Add(info);
        spriteAsset.UpdateLookupTables();

        return spriteAsset;
    }
    public static GameObject FindTargetUIObject(Transform root, string targetName)
    {
        // Stack to hold the transforms to be processed
        Stack<(Transform transform, int indentLevel)> transformStack = new();
        transformStack.Push((root, 0));

        // HashSet to keep track of visited transforms to avoid cyclic references
        HashSet<Transform> visited = [];

        Il2CppArrayBase<Transform> children = root.GetComponentsInChildren<Transform>(true);

        List<Transform> transforms = [.. children];

        while (transformStack.Count > 0)
        {
            var (current, indentLevel) = transformStack.Pop();

            if (!visited.Add(current))
            {
                // If we have already visited this transform, skip it
                continue;
            }

            if (current.gameObject.name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
            {
                // Return the transform if the name matches
                return current.gameObject;
            }

            // Create an indentation string based on the indent level
            //string indent = new('|', indentLevel);

            // Print the current GameObject's name and some basic info
            //Core.Log.LogInfo($"{indent}{current.gameObject.name} ({current.gameObject.scene.name})");

            // Add all children to the stack
            foreach (Transform child in transforms)
            {
                if (child.parent == current)
                {
                    transformStack.Push((child, indentLevel + 1));
                }
            }
        }

        Core.Log.LogWarning($"GameObject with name '{targetName}' not found!");
        return null;
    }
    public static void FindLoadedObjects<T>() where T : UnityEngine.Object
    {
        Il2CppReferenceArray<UnityEngine.Object> resources = UnityEngine.Resources.FindObjectsOfTypeAll(Il2CppType.Of<T>());
        Core.Log.LogInfo($"Found {resources.Length} {Il2CppType.Of<T>().FullName}'s!");
        foreach (UnityEngine.Object resource in resources)
        {
            Core.Log.LogInfo($"Sprite: {resource.name}");
        }
    }
    public static void DeactivateChildrenExceptNamed(Transform root, string targetName)
    {
        // Stack to hold the transforms to be processed
        Stack<(Transform transform, int indentLevel)> transformStack = new();
        transformStack.Push((root, 0));

        // HashSet to keep track of visited transforms to avoid cyclic references
        HashSet<Transform> visited = [];

        Il2CppArrayBase<Transform> children = root.GetComponentsInChildren<Transform>();
        List<Transform> transforms = [..children];

        while (transformStack.Count > 0)
        {
            var (current, indentLevel) = transformStack.Pop();

            if (!visited.Add(current))
            {
                // If we have already visited this transform, skip it
                continue;
            }

            // Add all children to the stack
            foreach (Transform child in transforms)
            {
                if (child.parent == current)
                {
                    transformStack.Push((child, indentLevel + 1));
                }

                if (!child.name.Equals(targetName)) child.gameObject.SetActive(false);
            }
        }
    }
    public static void FindGameObjects(Transform root, string filePath = "", bool includeInactive = false)
    {
        // Stack to hold the transforms to be processed
        Stack<(Transform transform, int indentLevel)> transformStack = new();
        transformStack.Push((root, 0));

        // HashSet to keep track of visited transforms to avoid cyclic references
        HashSet<Transform> visited = [];

        Il2CppArrayBase<Transform> children = root.GetComponentsInChildren<Transform>(includeInactive);
        List<Transform> transforms = [..children];

        Core.Log.LogWarning($"Found {transforms.Count} GameObjects!");

        if (string.IsNullOrEmpty(filePath))
        {
            while (transformStack.Count > 0)
            {
                var (current, indentLevel) = transformStack.Pop();

                if (!visited.Add(current))
                {
                    // If we have already visited this transform, skip it
                    continue;
                }

                List<string> objectComponents = FindGameObjectComponents(current.gameObject);

                // Create an indentation string based on the indent level
                string indent = new('|', indentLevel);

                // Write the current GameObject's name and some basic info to the file
                Core.Log.LogInfo($"{indent}{current.gameObject.name} | {string.Join(",", objectComponents)} | [{current.gameObject.scene.name}]");

                // Add all children to the stack
                foreach (Transform child in transforms)
                {
                    if (child.parent == current)
                    {
                        transformStack.Push((child, indentLevel + 1));
                    }
                }
            }
            return;
        }

        if (!File.Exists(filePath)) File.Create(filePath).Dispose();

        using StreamWriter writer = new(filePath, false);
        while (transformStack.Count > 0)
        {
            var (current, indentLevel) = transformStack.Pop();

            if (!visited.Add(current))
            {
                // If we have already visited this transform, skip it
                continue;
            }

            List<string> objectComponents = FindGameObjectComponents(current.gameObject);

            // Create an indentation string based on the indent level
            string indent = new('|', indentLevel);

            // Write the current GameObject's name and some basic info to the file
            writer.WriteLine($"{indent}{current.gameObject.name} | {string.Join(",", objectComponents)} | [{current.gameObject.scene.name}]");

            // Add all children to the stack
            foreach (Transform child in transforms)
            {
                if (child.parent == current)
                {
                    transformStack.Push((child, indentLevel + 1));
                }
            }
        }
    }
    public static List<string> FindGameObjectComponents(GameObject parentObject)
    {
        List<string> components = [];

        int componentCount = parentObject.GetComponentCount();
        for (int i = 0; i < componentCount; i++)
        {
            components.Add($"{parentObject.GetComponentAtIndex(i).GetIl2CppType().FullName}({i})");
        }

        return components;
    }
}

