using System.Collections.Generic;
using UnityEngine;

// Draw white alpha silhouettes behind the original icon in the same mesh.
// Unlike UI.Outline, BagIcon.shader ignores the texture's RGB for these copies.
[RequireComponent(typeof(UnityEngine.UI.Image))]
public class BagIconOutline : UnityEngine.UI.BaseMeshEffect
{
    [SerializeField, Min(0f)] private float width = 3f;
    private readonly List<UIVertex> source = new List<UIVertex>();
    private readonly List<UIVertex> outlined = new List<UIVertex>();

    protected override void OnEnable()
    {
        base.OnEnable();
        if (graphic != null && graphic.canvas != null)
            graphic.canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
    }

    public override void ModifyMesh(UnityEngine.UI.VertexHelper vertices)
    {
        if (!IsActive() || width <= 0f) return;
        source.Clear();
        outlined.Clear();
        vertices.GetUIVertexStream(source);

        for (int direction = 0; direction < 8; direction++)
        {
            float angle = direction * Mathf.PI / 4f;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * width;
            for (int i = 0; i < source.Count; i++)
            {
                UIVertex vertex = source[i];
                vertex.position += offset;
                vertex.color = new Color32(255, 255, 255, vertex.color.a);
                vertex.uv1 = new Vector4(1f, 0f, 0f, 0f);
                outlined.Add(vertex);
            }
        }

        for (int i = 0; i < source.Count; i++)
        {
            UIVertex vertex = source[i];
            vertex.uv1 = Vector4.zero;
            outlined.Add(vertex);
        }
        vertices.Clear();
        vertices.AddUIVertexTriangleStream(outlined);
    }
}
