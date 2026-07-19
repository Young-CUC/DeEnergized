using UnityEngine;

[RequireComponent(typeof(Light))]
[RequireComponent(typeof(MeshCollider))]
public class LightConeCollider : MonoBehaviour
{
    [Tooltip("圆锥的圆滑程度，数值越大越圆滑，但性能开销略微增加。8~16足够了。")]
    public int segments = 16;

    // 在 Inspector 脚本组件上右键，可以点击这个按钮手动更新
    [ContextMenu("Update Cone Collider")]
    public void UpdateCollider()
    {
        Light spotLight = GetComponent<Light>();
        MeshCollider meshCollider = GetComponent<MeshCollider>();

        if (spotLight.type != LightType.Spot)
        {
            Debug.LogWarning("该脚本只能用于 Spot Light！");
            return;
        }

        // 1. 获取灯光参数
        float length = spotLight.range;
        float angle = spotLight.spotAngle;

        // 2. 利用三角函数计算底面半径
        // 半径 = tan(角度的一半) * 长度
        float radius = Mathf.Tan(angle * 0.5f * Mathf.Deg2Rad) * length;

        // 3. 生成网格
        Mesh coneMesh = CreateConeMesh(length, radius, segments);

        // 4. 赋值给碰撞体
        meshCollider.sharedMesh = coneMesh;
        meshCollider.convex = true;
        meshCollider.isTrigger = true;

        Debug.Log("锥形碰撞体已完美贴合灯光！");
    }

    // 纯代码构建圆锥网格的核心逻辑
    private Mesh CreateConeMesh(float length, float radius, int segments)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Procedural_SpotLightCone";

        int numVertices = segments + 2;
        Vector3[] vertices = new Vector3[numVertices];
        int[] triangles = new int[segments * 6];

        // 顶点 0 是圆锥尖端 (光源原点)
        vertices[0] = Vector3.zero;
        // 顶点 1 是底面圆心
        vertices[1] = new Vector3(0, 0, length);

        // 计算底面边缘的顶点
        float angleStep = 360f / segments;
        for (int i = 0; i < segments; i++)
        {
            float rad = i * angleStep * Mathf.Deg2Rad;
            vertices[i + 2] = new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius, length);
        }

        // 构建侧面三角形
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = next + 2;
            triangles[i * 3 + 2] = i + 2;
        }

        // 构建底面三角形 (封口)
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            int baseIndex = segments * 3 + i * 3;
            triangles[baseIndex] = 1;
            triangles[baseIndex + 1] = i + 2;
            triangles[baseIndex + 2] = next + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}