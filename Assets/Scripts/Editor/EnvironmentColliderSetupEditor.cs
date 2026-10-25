#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EnvironmentColliderSetupEditor
{
    [MenuItem("Tools/Setup Environment Colliders")]
    public static void SetupCollidersInEditor()
    {
        MeshFilter[] meshFilters = UnityEngine.Object.FindObjectsOfType<MeshFilter>();
        int addedCount = 0;

        foreach (MeshFilter mf in meshFilters)
        {
            if (mf == null || mf.gameObject == null) continue;
            if (mf.GetComponent<Collider>() != null) continue; // Already has collider
            if (mf.sharedMesh == null || mf.sharedMesh.vertexCount == 0) continue;

            string nameLower = mf.gameObject.name.ToLower();
            if (nameLower.Contains("ocean") || nameLower.Contains("water")) continue;

            MeshCollider mc = Undo.AddComponent<MeshCollider>(mf.gameObject);
            mc.sharedMesh = mf.sharedMesh;
            addedCount++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[EnvironmentColliderSetup] Successfully added {addedCount} MeshColliders to scene meshes.");
        EditorUtility.DisplayDialog("Environment Colliders", $"Berhasil menambahkan {addedCount} MeshCollider ke elemen tanah dan rintangan!", "OK");
    }
}
#endif
