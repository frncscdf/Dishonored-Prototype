using UnityEngine;

public class Map : MonoBehaviour
{
    [SerializeField]
    private Gradient Gradient;
    [SerializeField]
    private Material Mat;
    [SerializeField]
    private LayerMask PropsColliderMask;

    private MaterialPropertyBlock m_MaterialProperyBlock;

    public void Init(float playerHeight)
    {
        ColorMap();
        BoxCollider[] colliders = GenerateColliders();
        AdjustPropsColliders(colliders, playerHeight);
    }
    private void ColorMap()
    {
        int child_count = transform.childCount;
        for (int i = 1; i < child_count; ++i)
        {
            Transform g = transform.GetChild(i);
            if (g.name.Equals("Props"))
            {
                for (int j = 0; j < g.childCount; j++)
                {
                    Transform t = g.GetChild(j);
                    if (t.CompareTag("Prop"))
                    {
                        SetColor(Gradient.Evaluate(Random.Range(0f, 1f)), t.GetComponent<Renderer>());
                    }
                }
            }
            else
            {
                if (g.CompareTag("Prop"))
                {
                    SetColor(Gradient.Evaluate(Random.Range(0f, 1f)), g.GetComponent<Renderer>());
                }
            }
        }
    }

    private void SetColor(Color color, Renderer renderer)
    {
        if (m_MaterialProperyBlock == null)
            m_MaterialProperyBlock = new MaterialPropertyBlock();

        m_MaterialProperyBlock.SetColor("_Color", color);
        renderer.SetPropertyBlock(m_MaterialProperyBlock);
    }

    private BoxCollider[] GenerateColliders()
    {
        Transform propsHolder = transform.Find("Props");
        BoxCollider[] colliders = new BoxCollider[propsHolder.childCount];
        if (propsHolder != null)
        {
            colliders = new BoxCollider[propsHolder.childCount];

            for (int i = 0; i < propsHolder.childCount; i++)
            {
                GameObject g = new GameObject("Collider");
                g.transform.SetParent(propsHolder.GetChild(i).transform, false);
                g.layer = LayerMask.NameToLayer("Prop");
                g.tag = "Prop";
                BoxCollider bc = g.AddComponent<BoxCollider>();
                colliders[i] = bc;
            }
        }

        return colliders;
    }

    private void AdjustPropsColliders(BoxCollider[] colliders, float playerHeight)
    {
        for (int i = 0; i < colliders.Length; i++)
        {
            BoxCollider collider = colliders[i];

            float halfScale = collider.transform.parent.transform.localScale.y / 2f;
            float dst = playerHeight;

            RaycastHit rayDown;
            Ray ray = new Ray(collider.transform.parent.transform.position + Vector3.down * halfScale, Vector3.down);
            Physics.Raycast(ray, out rayDown, dst + 1f, PropsColliderMask);

            if (rayDown.distance > 0 && rayDown.distance < dst)
            {
                // in world space
                Bounds bounds = collider.bounds;

                Vector3 size = bounds.size + Vector3.up * rayDown.distance;
                Vector3 center = bounds.center - Vector3.up * rayDown.distance / 2;

                // transform in local space
                size = collider.transform.InverseTransformVector(size);
                size = new Vector3(1f, Mathf.Abs(size.y), 1f);
                center = collider.transform.InverseTransformPoint(center);

                collider.size = size;
                collider.center = center;
            }
        }
    }
}
