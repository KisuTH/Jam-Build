using UnityEngine;

namespace JamBuild.Options
{
    /// <summary>Paper with optional artwork, plus resolution-independent UI marks.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class NotebookPaper : UnityEngine.UI.MaskableGraphic
    {
        public enum Shape { Paper, Rounded, Tick, Triangle, Shadow }
        [SerializeField] private Shape shape;
        [SerializeField] private bool spineOnLeft;
        [SerializeField] private float radius = 12f;
        [SerializeField] private Texture2D paperTexture;
        [SerializeField] private Rect textureUV = new Rect(0, 0, 1, 1);
        private float bend;

        public override Texture mainTexture => shape == Shape.Paper && paperTexture != null
            ? paperTexture : base.mainTexture;

        public void SetPaperArtwork(Texture2D texture, Rect uv)
        {
            paperTexture = texture;
            textureUV = uv;
            color = Color.white;
            SetAllDirty();
        }

        public void Configure(Shape value, Color tint, bool leftSpine = false, float corner = 12f)
        {
            shape = value; color = tint; spineOnLeft = leftSpine; radius = corner;
            raycastTarget = false; SetVerticesDirty();
        }

        public void SetBend(float value) { bend = value; SetVerticesDirty(); }

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();
            Rect r = rectTransform.rect;
            if (shape == Shape.Tick)
            {
                Stroke(vh, Point(r, .12f, .45f), Point(r, .36f, .18f), 5f, color);
                Stroke(vh, Point(r, .36f, .18f), Point(r, .93f, .91f), 5f, color);
                return;
            }
            if (shape == Shape.Triangle)
            {
                vh.AddVert(Point(r, 0, 1), color, Vector2.zero);
                vh.AddVert(Point(r, 1, 1), color, Vector2.zero);
                vh.AddVert(Point(r, .5f, 0), color, Vector2.zero);
                vh.AddTriangle(0, 1, 2); return;
            }
            if (shape == Shape.Rounded)
            {
                float rad = Mathf.Min(radius, Mathf.Min(r.width, r.height) * .5f);
                vh.AddVert(r.center, color, Vector2.zero);
                for (int corner = 0; corner < 4; corner++)
                {
                    Vector2 center = new Vector2(corner < 2 ? r.xMax-rad : r.xMin+rad,
                        corner == 0 || corner == 3 ? r.yMax-rad : r.yMin+rad);
                    for (int step = 0; step <= 6; step++)
                    {
                        float a = (90f - corner * 90f - step * 15f) * Mathf.Deg2Rad;
                        vh.AddVert(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a))*rad, color, Vector2.zero);
                    }
                }
                for (int i = 1; i <= 28; i++) vh.AddTriangle(0, i, i == 28 ? 1 : i + 1);
                return;
            }
            const int strips = 32;
            for (int i = 0; i <= strips; i++)
            {
                float u = (float)i / strips;
                float distance = spineOnLeft ? u : 1f-u;
                float fold = Mathf.Exp(-distance * 22f);
                bool textured = shape == Shape.Paper && paperTexture != null;
                float shading = 1f - (textured ? 0 : fold * .14f) - Mathf.Sin(distance*Mathf.PI)*bend*.12f;
                Color tint = new Color(color.r*shading, color.g*shading, color.b*shading, color.a);
                if(shape==Shape.Shadow) tint=new Color(color.r,color.g,color.b,color.a*Mathf.Sin(u*Mathf.PI));
                float bow = Mathf.Sin(distance * Mathf.PI) * bend * 13f;
                float edgeDistance=(spineOnLeft?1f-u:u)*r.width;
                float corner=shape==Shape.Paper&&!textured&&edgeDistance<12f?12f-Mathf.Sqrt(144f-Mathf.Pow(12f-edgeDistance,2)):0;
                float x = Mathf.Lerp(r.xMin,r.xMax,u);
                float textureX = Mathf.Lerp(textureUV.xMin, textureUV.xMax, u);
                vh.AddVert(new Vector2(x,r.yMin+bow+corner), tint, textured ? new Vector2(textureX, textureUV.yMin) : Vector2.zero);
                vh.AddVert(new Vector2(x,r.yMax-bow-corner), tint, textured ? new Vector2(textureX, textureUV.yMax) : Vector2.zero);
                if (i == 0) continue;
                int n=i*2; vh.AddTriangle(n-2,n-1,n); vh.AddTriangle(n,n-1,n+1);
            }
        }

        private static Vector2 Point(Rect r,float x,float y) => new Vector2(r.xMin+r.width*x,r.yMin+r.height*y);
        private static void Stroke(UnityEngine.UI.VertexHelper vh,Vector2 a,Vector2 b,float width,Color tint)
        {
            Vector2 d=(b-a).normalized; Vector2 n=new Vector2(-d.y,d.x)*width*.5f;
            int start=vh.currentVertCount;
            vh.AddVert(a-n,tint,Vector2.zero); vh.AddVert(a+n,tint,Vector2.zero);
            vh.AddVert(b+n,tint,Vector2.zero); vh.AddVert(b-n,tint,Vector2.zero);
            vh.AddTriangle(start,start+1,start+2); vh.AddTriangle(start,start+2,start+3);
        }
    }
}
